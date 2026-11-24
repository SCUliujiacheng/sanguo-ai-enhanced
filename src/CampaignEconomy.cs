using System;
using System.Collections.Generic;
using System.Xml;

public static class CampaignEconomy
{
	private const int CityMoneyCap = 999999;

	// Keep hundredths with the individual resource. A +1 base gain is worth
	// four whole points over five easy ticks, or six over five hard ticks.
	private static int ApplyGain(int current, int maximum, int baseGain, int percent, ref int remainder)
	{
		if (current >= maximum)
		{
			remainder = 0;
			return current;
		}
		long hundredths = (long)Math.Max(0, baseGain) * percent;
		if (percent != 100) hundredths += remainder;
		long next = current + hundredths / 100;
		if (next >= maximum)
		{
			remainder = 0;
			return maximum;
		}
		remainder = (int)(hundredths % 100);
		return (int)Math.Max(0L, next);
	}

	public static void RecoverGeneral(GeneralInfo general)
	{
		int percent = CampaignDifficulty.PercentFor(general.king);
		general.healthCur = ApplyGain(general.healthCur, general.healthMax, general.strength / 20,
			percent, ref general.healthRecoveryRemainder);
		general.manaCur = ApplyGain(general.manaCur, general.manaMax, general.intellect / 20,
			percent, ref general.manaRecoveryRemainder);
	}

	public static void CollectAnnualIncome(CityInfo city)
	{
		int baseIncome = (int)(2250f + (float)city.population / 133.34f);
		city.money = ApplyGain(city.money, CityMoneyCap, baseIncome, CampaignDifficulty.PercentFor(city.king),
			ref city.incomeRemainder);
	}

	public static int ReadRemainder(XmlElement node, string name)
	{
		int value;
		return int.TryParse(node.GetAttribute(name), out value) && value > 0 && value < 100 ? value : 0;
	}

	public static void WriteRemainder(XmlElement node, string name, int value)
	{
		if (value > 0 && value < 100) node.SetAttribute(name, value.ToString());
	}

	private static bool CanRemit(Informations world, int index, int player)
	{
		CityInfo city = world.GetCityInfo(index);
		if (city.king != player || city.money <= 0 || city.reservistMax <= 0 || city.reservist < city.reservistMax)
			return false;
		List<int> neighbors = MyPathfinding.GetCityNearbyIdx(index);
		if (neighbors == null) return false;
		for (int n = 0; n < neighbors.Count; n++)
		{
			CityInfo neighbor = world.GetCityInfo(neighbors[n]);
			if (neighbor != null && neighbor.king >= 0 && neighbor.king < world.kingNum && neighbor.king != player)
				return false;
		}
		return true;
	}

	public static void RemitInteriorGold()
	{
		Informations world = Informations.Instance;
		int player = Controller.kingIndex;
		if (player < 0 || player >= world.kingNum) return;
		KingInfo king = world.GetKingInfo(player);
		if (king == null || king.active == 0) return;
		GeneralInfo ruler = world.GetGeneralInfo(king.generalIdx);
		if (ruler == null || ruler.king != player || ruler.prisonerIdx != -1) return;
		int seat = ruler.city;
		CityInfo capital = world.GetCityInfo(seat);
		if (capital == null || capital.king != player || capital.money >= CityMoneyCap ||
			capital.generals == null || !capital.generals.Contains(king.generalIdx)) return;

		// Usually no full interior city has money left on the next recruiting
		// tick. Avoid building a road graph for those ticks.
		int candidate = 0;
		while (candidate < world.cityNum && (candidate == seat || !CanRemit(world, candidate, player))) candidate++;
		if (candidate == world.cityNum) return;

		bool[] closed = new bool[world.cityNum];
		for (int i = 0; i < world.armys.Count; i++)
		{
			ArmyInfo army = world.armys[i];
			if (army == null) continue;
			if ((army.generals != null && army.generals.Contains(king.generalIdx)) ||
				(army.prisons != null && army.prisons.Contains(king.generalIdx))) return;
			if (army.king < 0 || army.king >= world.kingNum || army.king == player ||
				army.generals == null || army.generals.Count == 0 || army.cityTo < 0 || army.cityTo >= closed.Length) continue;
			// A live army's component has the current state. The ArmyInfo copy is
			// synchronized on scene unload and is authoritative between scenes.
			int state = (object)army.armyCtrl == null ? army.state : (int)army.armyCtrl.GetState();
			if (state != (int)ArmyController.ArmyState.Garrison) closed[army.cityTo] = true;
		}
		if (closed[seat]) return;

		// Walk outward only through ours, skipping threatened cities. A blocked
		// shortest road still allows delivery along a different friendly road.
		closed[seat] = true;
		List<int> reachable = new List<int>();
		reachable.Add(seat);
		for (int i = 0; i < reachable.Count && capital.money < CityMoneyCap; i++)
		{
			int index = reachable[i];
			if (index != seat && CanRemit(world, index, player))
			{
				CityInfo donor = world.GetCityInfo(index);
				int amount = (int)Math.Min((long)donor.money, (long)CityMoneyCap - capital.money);
				donor.money -= amount;
				capital.money += amount;
			}
			List<int> neighbors = MyPathfinding.GetCityNearbyIdx(index);
			if (neighbors == null) continue;
			for (int n = 0; n < neighbors.Count; n++)
			{
				int next = neighbors[n];
				if (next < 0 || next >= closed.Length || closed[next]) continue;
				CityInfo nextCity = world.GetCityInfo(next);
				if (nextCity == null || nextCity.king != player) continue;
				closed[next] = true;
				reachable.Add(next);
			}
		}
	}
}
