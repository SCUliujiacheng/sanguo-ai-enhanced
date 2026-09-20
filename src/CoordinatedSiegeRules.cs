using System;
using System.Collections.Generic;

// Evaluate real waves conservatively: later waves lose a quarter of their value
// to travel, interception and recovery between battles. No prisoner rescue bonus.
public static class CoordinatedSiegeRules
{
	public static bool CanAssaultAlone(int power, int targetPower, int count, int defenders)
	{
		if (StrategicAiMath.CanLaunchConcentratedAttack(power, targetPower, count, defenders)) return true;
		// A strong finishing force may exploit defenders worn down by an earlier wave.
		return count >= Math.Max(3, (defenders + 1) / 2) && targetPower > 0
			&& (long)power * 100 >= (long)targetPower * 160;
	}

	public static bool CanCommit(int[] powers, int[] counts, int targetPower, int defenders)
	{
		if (powers == null || counts == null || powers.Length != counts.Length || powers.Length < 2 || powers.Length > 3 || targetPower <= 0 || defenders < 2) return false;
		long total = 0;
		int strongest = 0;
		int totalCount = 0;
		int minimumCount = Math.Max(2, (defenders + 2) / 3);
		for (int i = 0; i < powers.Length; i++)
		{
			if (counts[i] < minimumCount || counts[i] > 10 || powers[i] <= 0 || (long)powers[i] * 4 < targetPower) return false;
			total += powers[i];
			strongest = Math.Max(strongest, powers[i]);
			totalCount += counts[i];
		}
		return totalCount >= defenders && (total * 3 + strongest) * 25 >= (long)targetPower * 135;
	}

	public static int GetArmyPower(ArmyInfo army)
	{
		if (army == null || army.generals == null) return 0;
		int power = 0;
		for (int i = 0; i < army.generals.Count; i++)
		{
			GeneralInfo general = Informations.Instance.GetGeneralInfo(army.generals[i]);
			if (general == null || general.king != army.king || general.prisonerIdx != -1 || general.healthCur <= 0) return 0;
			power += StrategicAiMath.EstimateGeneralPower(general.level, general.strength, general.intellect, general.healthCur, general.manaCur, general.soldierCur, general.knightCur);
		}
		return power;
	}

	public static int GetTargetPower(CityInfo target)
	{
		if (target == null || target.generals == null) return 0;
		int power = Math.Max(0, target.defense) / 10;
		for (int i = 0; i < target.generals.Count; i++)
		{
			GeneralInfo general = Informations.Instance.GetGeneralInfo(target.generals[i]);
			if (general != null) power += StrategicAiMath.EstimateGeneralPower(general.level, general.strength, general.intellect, general.healthCur, general.manaCur, general.soldierCur, general.knightCur);
		}
		return power;
	}

	// 0 = retreat, 1 = attack, 2 = wait for actual marching support.
	public static int GetArrivalDecision(ArmyInfo army, CityInfo target)
	{
		if (army == null || target == null || army.generals == null) return 0;
		if (army.king == Controller.kingIndex || target.king < 0 || target.king == army.king || target.generals == null || target.generals.Count == 0) return 1;
		if ((object)army.armyCtrl != null && army.armyCtrl.GetState() == ArmyController.ArmyState.Escape) return 0;
		int power = GetArmyPower(army);
		int defense = GetTargetPower(target);
		if (CanAssaultAlone(power, defense, army.generals.Count, target.generals.Count)) return 1;
		if (army.cityTo < 0 || Informations.Instance.GetCityInfo(army.cityTo) != target) return 0;
		if (HasSupport(army, power, defense, target.generals.Count, 30f)) return 1;
		return HasSupport(army, power, defense, target.generals.Count, 180f) ? 2 : 0;
	}

	private static bool HasSupport(ArmyInfo army, int power, int defense, int defenders, float remainingDistance)
	{
		List<int> powers = new List<int>();
		List<int> counts = new List<int>();
		List<int> used = new List<int>(army.generals);
		powers.Add(power);
		counts.Add(army.generals.Count);
		List<ArmyInfo> candidates = new List<ArmyInfo>();
		for (int i = 0; i < Informations.Instance.armys.Count; i++)
		{
			ArmyInfo other = Informations.Instance.armys[i];
			if (other == army || other.king != army.king || other.cityTo != army.cityTo || other.armyCtrl == null || other.generals == null || other.armyCtrl.GetState() != ArmyController.ArmyState.Running) continue;
			float distance = other.armyCtrl.GetSiegeRemainingDistance();
			if (distance < 0f || float.IsNaN(distance) || distance > remainingDistance) continue;
			int otherPower = GetArmyPower(other);
			if (other.generals.Count < Math.Max(2, (defenders + 2) / 3) || (long)otherPower * 4 < defense) continue;
			int at = 0;
			while (at < candidates.Count && GetArmyPower(candidates[at]) >= otherPower) at++;
			candidates.Insert(at, other);
		}
		for (int i = 0; i < candidates.Count && powers.Count < 3; i++)
		{
			ArmyInfo other = candidates[i];
			bool duplicate = false;
			for (int j = 0; j < other.generals.Count; j++) if (used.Contains(other.generals[j])) duplicate = true;
			if (duplicate) continue;
			used.AddRange(other.generals);
			powers.Add(GetArmyPower(other));
			counts.Add(other.generals.Count);
			if (CanCommit(powers.ToArray(), counts.ToArray(), defense, defenders)) return true;
		}
		return false;
	}
}
