// Resolve presentation from the current ruler, never from a scenario's faction
// slot. This also follows a changed ruler in saved games.
public static class RulerIdentityRules
{
    public static int GetGeneralIndex(int kingIndex)
    {
        Informations information = Informations.Instance;
        if (kingIndex < 0 || kingIndex >= information.kingNum) return -1;
        KingInfo king = information.GetKingInfo(kingIndex);
        if (king == null || king.generalIdx < 0 || king.generalIdx >= information.generalNum) return -1;
        return king.generalIdx;
    }

    public static string GetName(int kingIndex)
    {
        int general = GetGeneralIndex(kingIndex);
        return general < 0 ? string.Empty : ZhongWen.Instance.GetGeneralName(general);
    }

    public static string GetHeadResource(int kingIndex)
    {
        int general = GetGeneralIndex(kingIndex);
        return general < 0 ? string.Empty : "Head/Head" + (general + 1).ToString("D3");
    }
}
