// Reuse the game's animated surname banners. Resolve every faction, including
// rulers introduced by extra scenarios, before touching either map renderer.
public static class FactionFlagRules
{
    public static int GetPreferredFlag(string name)
    {
        string[] originals = ZhongWen.Instance.kingNames;
        for (int i = 0; i < originals.Length; i++)
            if (!string.IsNullOrEmpty(name) && name == originals[i]) return i + 1;
        switch (name)
        {
            case "马超": return 11; // Ma Teng's Ma banner
            case "曹丕": case "曹叡": return 1;
            case "孙策": return 3;
            case "公孙康": return 7;
            case "吕布": return 19; // distinct legacy unaffiliated banner
            default: return 0;
        }
    }

    public static int GetFlagIndex(int kingIndex)
    {
        int count = Informations.Instance.kingNum;
        // The original game assigns dissolved factions to the bandit slot,
        // distinct from an empty city (-1). Preserve its visible Flag19.
        if (kingIndex == count) return 19;
        if (kingIndex < 0 || kingIndex >= count) return 0;
        int[] assigned = new int[count];
        bool[] used = new bool[31];
        // Reserve known banners first; a fallback must never steal another
        // faction's banner merely because it precedes it in the scenario file.
        for (int i = 0; i < count; i++)
        {
            int preferred = GetPreferredFlag(ZhongWen.Instance.GetKingName(i));
            if (preferred > 0 && preferred < used.Length && !used[preferred])
            {
                assigned[i] = preferred;
                used[preferred] = true;
            }
        }
        for (int i = 0; i < count; i++)
        {
            if (assigned[i] != 0) continue;
            for (int flag = 1; flag < used.Length; flag++)
                if (!used[flag]) { assigned[i] = flag; used[flag] = true; break; }
        }
        return assigned[kingIndex];
    }

    public static string GetAnimationName(int kingIndex)
    {
        int flag = GetFlagIndex(kingIndex);
        return flag == 0 ? string.Empty : "Flag" + flag;
    }
}
