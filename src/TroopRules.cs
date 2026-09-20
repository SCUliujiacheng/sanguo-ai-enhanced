using System.Collections.Generic;

/// <summary>Three playable troop families, retaining the original prefab and save bit IDs.</summary>
public static class TroopRules
{
	public const int Blade = 1;
	public const int Spear = 2;
	public const int Archer = 8;
	public const int PlayableMask = Blade | Spear | Archer;

	public static int MapLegacyIndex(int index)
	{
		switch (index)
		{
		case 1:
		case 2:
		case 7:
			return Spear;
		case 3:
		case 4:
		case 5:
			return Archer;
		default:
			return Blade;
		}
	}

	public static int NormalizeArms(int arms)
	{
		int result = 0;
		for (int i = 0; i < 11; i++)
		{
			if ((arms & (1 << i)) != 0)
			{
				result |= MapLegacyIndex(i);
			}
		}
		return result == 0 ? Blade : result;
	}

	public static int NormalizeCurrentArms(int current, int known)
	{
		int available = NormalizeArms(known);
		int desired = current == 0 ? 0 : NormalizeArms(current) & available;
		if (desired == 0)
		{
			desired = available;
		}
		if ((desired & Blade) != 0) return Blade;
		if ((desired & Spear) != 0) return Spear;
		return Archer;
	}

	public static int GetArmsIndex(int arms)
	{
		int current = NormalizeCurrentArms(arms, arms);
		return current == Archer ? 3 : current == Spear ? 1 : 0;
	}

	public static bool IsPlayableIndex(int index)
	{
		return index == 0 || index == 1 || index == 3;
	}

	public static int GetRecruitableArms(int choice)
	{
		return choice == 0 ? Archer : choice == 1 ? Spear : Blade;
	}

	public static string GetName(int arms)
	{
		if (arms == 0) return null;
		int index = GetArmsIndex(arms);
		return index == 3 ? "弓箭兵" : index == 1 ? "长枪兵" : "朴刀兵";
	}

	public static float GetCounterMultiplier(int attackerArms, int defenderArms)
	{
		int attacker = NormalizeCurrentArms(attackerArms, attackerArms);
		int defender = NormalizeCurrentArms(defenderArms, defenderArms);
		if (attacker == defender) return 1f;
		if ((attacker == Archer && defender == Spear)
			|| (attacker == Spear && defender == Blade)
			|| (attacker == Blade && defender == Archer)) return 1.25f;
		return 0.8f;
	}

	public static int GetTroopHitChance(int attackerArms, int defenderArms)
	{
		float multiplier = GetCounterMultiplier(attackerArms, defenderArms);
		return multiplier > 1f ? 63 : multiplier < 1f ? 40 : 50;
	}

	public static int NormalizeItem(int code)
	{
		if ((code >> 16) != 2) return code;
		int arms = code & 0xffff;
		return 131072 | NormalizeCurrentArms(arms, arms);
	}

	public static void NormalizeInventory(List<int> objects)
	{
		if (objects == null) return;
		for (int i = 0; i < objects.Count; i++) objects[i] = NormalizeItem(objects[i]);
	}

	public static void NormalizeGeneral(GeneralInfo general)
	{
		if (general == null) return;
		// A currently fielded family is also a learned family in valid legacy saves.
		// Preserve it when repairing a save whose known-arms mask omitted that bit.
		int known = general.arms;
		if (general.armsCur != 0) known |= general.armsCur;
		general.arms = NormalizeArms(known);
		general.armsCur = NormalizeCurrentArms(general.armsCur, general.arms);
		// Preserve both totals exactly, including generals currently marching or captured.
		// Clearing the legacy tier makes this conversion idempotent across repeated loads.
		general.soldierCur += general.knightCur;
		general.soldierMax += general.knightMax;
		general.knightCur = 0;
		general.knightMax = 0;
	}

	public static bool LearnArms(GeneralInfo general, int arms)
	{
		if (general == null) return false;
		NormalizeGeneral(general);
		int learned = NormalizeCurrentArms(arms, arms);
		if ((general.arms & learned) != 0) return false;
		general.arms |= learned;
		return true;
	}

	public static void NormalizeWorld(Informations informations)
	{
		if (informations == null) return;
		for (int i = 0; i < informations.generalNum; i++) NormalizeGeneral(informations.GetGeneralInfo(i));
		for (int i = 0; i < informations.cityNum; i++)
		{
			CityInfo city = informations.GetCityInfo(i);
			if (city != null) NormalizeInventory(city.objects);
		}
	}
}
