using System;
using System.IO;
using System.Reflection;
using System.Xml;

// Search eligibility only: never moves an employed general or changes a prisoner.
public static class HistoricalRecruitmentRules
{
    private static XmlElement[] rules;

    public static bool CanRecruit(int generalIdx, int cityIdx, int kingIdx, int year)
    {
        Informations info = Informations.Instance;
        if (!ScenarioCatalog.IsValidIndex(Controller.MODSelect) ||
            generalIdx < 0 || generalIdx >= info.generalNum ||
            cityIdx < 0 || cityIdx >= info.cityNum || kingIdx < 0 || kingIdx >= info.kingNum)
            return false;
        GeneralInfo candidate = info.GetGeneralInfo(generalIdx);
        if (candidate == null || candidate.king != -1 || candidate.prisonerIdx != -1)
            return false;
        CityInfo city = info.GetCityInfo(cityIdx);
        KingInfo recruiter = info.GetKingInfo(kingIdx);
        if (city == null || city.king != kingIdx || recruiter == null || recruiter.active == 0)
            return false;
        // The two explicitly fictional challenges retain an unrestricted free-agent pool.
        if (Controller.MODSelect == 12 || Controller.MODSelect == 13) return true;

        XmlElement rule = LoadRules()[generalIdx];
        foreach (XmlElement stage in rule.SelectNodes("Stage"))
        {
            if (year < int.Parse(stage.GetAttribute("fromYear")) ||
                year > int.Parse(stage.GetAttribute("toYear")) ||
                !IncludesId(stage.GetAttribute("cities"), cityIdx)) continue;

            string rulers = stage.GetAttribute("rulers");
            if (rulers == "*") return true;
            bool historicalFactionSurvives = false;
            for (int i = 0; i < info.kingNum; i++)
            {
                KingInfo king = info.GetKingInfo(i);
                // A faction can survive with its ruler's army after losing its last
                // city. Only the game's elimination flag ends its historical claim.
                if (king == null || king.active == 0 || !IncludesId(rulers, king.generalIdx)) continue;
                historicalFactionSurvives = true;
                if (i == kingIdx) return true;
            }
            // User-selected fallback: the local occupier may recruit only after all
            // eligible historical factions are gone. Year and region still apply.
            if (!historicalFactionSurvives) return true;
        }
        return false;
    }

    private static bool IncludesId(string ids, int wanted)
    {
        if (ids == "*") return true;
        string[] parts = ids.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            int parsed;
            if (int.TryParse(parts[i], out parsed) && parsed == wanted) return true;
        }
        return false;
    }

    private static XmlElement[] LoadRules()
    {
        if (rules != null) return rules;
        XmlDocument document = new XmlDocument();
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scenarios.HistoricalRecruitment.xml"))
        {
            if (stream == null) throw new InvalidOperationException("Historical recruitment rules are missing.");
            document.Load(stream);
        }
        XmlElement[] loaded = new XmlElement[Informations.Instance.generalNum];
        foreach (XmlElement element in document.DocumentElement.SelectNodes("General"))
        {
            int id = int.Parse(element.GetAttribute("id"));
            if (id < 0 || id >= loaded.Length || loaded[id] != null)
                throw new InvalidOperationException("Invalid or duplicate historical recruitment ID.");
            loaded[id] = element;
        }
        for (int i = 0; i < loaded.Length; i++)
            if (loaded[i] == null) throw new InvalidOperationException("Missing historical recruitment ID " + i);
        rules = loaded;
        return rules;
    }
}
