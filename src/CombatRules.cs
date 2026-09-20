using System;

// These charge rules are used by the visible battle meter and the auto resolver.
public static class CombatRules
{
    public static float GetCooldownMultiplier(int intellect)
    {
        return 1f - Math.Min(45, Math.Max(0, intellect - 60)) / 100f;
    }

    public static float GetMagicChargePerSecond(int strength, int intellect)
    {
        int ability = Math.Min(120, Math.Max(0, Math.Max(strength, intellect)));
        return (ability / 30f + 3f) / GetCooldownMultiplier(intellect);
    }

    public static int GetMagicCooldownTicks(int strength, int intellect, bool firstCast)
    {
        // The actual battle begins with 60 / 132 charge. One simulation tick is 0.1 s.
        return Math.Max(1, (int)Math.Ceiling((firstCast ? 72f : 132f) * 10f / GetMagicChargePerSecond(strength, intellect)));
    }

    public static AutoBattleSkill BuildSkill(MagicDataInfo data)
    {
        if (data == null || data.MP <= 0) return null;
        AutoBattleSkill skill = new AutoBattleSkill();
        skill.id = data.SEQUENCE;
        skill.cost = data.MP;
        int tier = 1;
        string note = data.NOTE ?? string.Empty;
        if (note.IndexOf("L4") >= 0) tier = 4;
        else if (note.IndexOf("L3") >= 0) tier = 3;
        else if (note.IndexOf("L2") >= 0) tier = 2;
        string attrib = data.ATTRIB ?? string.Empty;
        string target = data.ACTIVE ?? string.Empty;
        if (attrib.IndexOf("补血") >= 0)
            skill.healing = Math.Max(0, data.ATTACK);
        else if (attrib.IndexOf("补兵") >= 0)
            skill.reinforcement = tier == 1 ? 4 : tier == 2 ? 8 : tier == 3 ? 16 : 30;
        else if (data.SEQUENCE == 39)
            skill.disableTicks = 50;
        else
        {
            if (target.IndexOf("主将") >= 0 || target.IndexOf("全军") >= 0)
                skill.healthDamage = Math.Max(0, data.ATTACK);
            // POWER is 100 for every original skill: it is not damage. Area coverage
            // is an estimate for the fast resolver, scaled by the existing skill tier.
            if (target.IndexOf("士兵") >= 0 || target.IndexOf("全军") >= 0)
                skill.troopDamage = 6 + tier * 6;
            if (attrib.IndexOf("四周") >= 0 || attrib.IndexOf("中间") >= 0 || attrib.IndexOf("两侧") >= 0)
                skill.troopDamage = (skill.troopDamage * 3 + 3) / 4;
        }
        return skill;
    }

    public static int GetSkillValue(AutoBattleSkill skill, int mana, int health, int healthMax,
        int troops, int troopMax, int enemyHealth, int enemyTroops)
    {
        if (skill == null || skill.cost <= 0 || mana < skill.cost || health <= 0 || enemyHealth <= 0) return 0;
        int value = Math.Min(Math.Max(0, enemyHealth), skill.healthDamage) * 2;
        value += Math.Min(Math.Max(0, enemyTroops), skill.troopDamage) * 3;
        int missingHealth = Math.Max(0, healthMax - health);
        if (skill.healing > 0 && missingHealth >= Math.Min(10, skill.healing))
            value += Math.Min(missingHealth, skill.healing) * (health * 3 < healthMax ? 4 : 2);
        int missingTroops = Math.Max(0, troopMax - troops);
        if (skill.reinforcement > 0 && missingTroops >= Math.Min(4, skill.reinforcement))
            value += Math.Min(missingTroops, skill.reinforcement) * 3;
        if (skill.disableTicks > 0 && enemyTroops > 0)
            value += Math.Min(40, enemyTroops) * (troops > 0 ? 2 : 1);
        // Favor useful effects per MP, while giving an immediate finishing blow priority.
        int score = value * 100 / Math.Max(10, skill.cost);
        if (skill.healthDamage >= enemyHealth) score += 1000;
        return score;
    }
}
