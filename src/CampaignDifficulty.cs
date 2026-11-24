using System.Xml;

public static class CampaignDifficulty
{
    private static int aiPercent = 100;

    public static void Set(int percent)
    {
        aiPercent = percent == 80 || percent == 120 ? percent : 100;
    }

    public static int PercentFor(int kingId)
    {
        if (aiPercent == 100 || kingId == Controller.kingIndex || kingId < 0 || kingId >= Informations.Instance.kingNum)
            return 100;
        return aiPercent;
    }

    public static void Save(XmlElement mod)
    {
        mod.SetAttribute("Difficulty", aiPercent == 80 ? "easy" : aiPercent == 120 ? "hard" : "normal");
    }

    public static void Restore(XmlElement mod)
    {
        string level = mod == null ? string.Empty : mod.GetAttribute("Difficulty");
        Set(level == "easy" ? 80 : level == "hard" ? 120 : 100);
    }
}
