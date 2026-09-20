// CPU-versus-CPU duels use the same troop counters, skill choice, charge time,
// mana consumption and casualty simulation as the player's automatic battles.
public static class StrategicBattleRules
{
	public static AutoBattleUnit BuildUnit(GeneralInfo general, int defense)
	{
		AutoBattleUnit unit = new AutoBattleUnit();
		unit.level = general.level;
		unit.strength = general.strength;
		unit.intellect = general.intellect;
		unit.health = general.healthCur;
		unit.healthMax = general.healthMax;
		unit.mana = general.manaCur;
		unit.manaMax = general.manaMax;
		unit.soldier = general.soldierCur;
		unit.soldierMax = general.soldierMax;
		unit.knight = general.knightCur;
		unit.knightMax = general.knightMax;
		unit.arms = TroopRules.NormalizeCurrentArms(general.armsCur, general.arms);
		unit.formation = general.formationCur;
		unit.position = 0;
		unit.defense = defense;
		unit.skills = new AutoBattleSkill[general.magic == null ? 0 : general.magic.Length];
		for (int i = 0; i < unit.skills.Length; i++)
		{
			if (general.magic[i] >= 0) unit.skills[i] = CombatRules.BuildSkill(MagicManager.Instance.GetMagicDataInfo(general.magic[i]));
		}
		return unit;
	}

	public static int ResolveDuel(GeneralInfo attacker, GeneralInfo defender, int defense, bool cityDefense, int seed)
	{
		AutoBattleDuelResult result = AutoBattleMath.Simulate(BuildUnit(attacker, 0), BuildUnit(defender, defense), false, cityDefense, seed);
		attacker.healthCur = result.leftHealth;
		attacker.manaCur = result.leftMana;
		attacker.soldierCur = result.leftSoldier;
		attacker.knightCur = result.leftKnight;
		defender.healthCur = result.rightHealth;
		defender.manaCur = result.rightMana;
		defender.soldierCur = result.rightSoldier;
		defender.knightCur = result.rightKnight;
		return result.winner;
	}
}
