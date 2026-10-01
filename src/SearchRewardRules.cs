using System;

// Seal searches serve real faction learning needs, rather than filling a city
// warehouse after the three troop families have already been learned.
public static class SearchRewardRules
{
    public const int SealSearchPercent = 8;

    public static int GetSearchOutcome(int roll)
    {
        roll = Math.Max(0, Math.Min(99, roll));
        if (roll < 20) return 0;
        if (roll < 40) return 1;
        if (roll < 60) return 2;
        if (roll < 68) return 3;
        if (roll < 80) return 0;
        return 4;
    }

    // The computer's existing item-only branch has no talent/nothing outcome.
    // Preserve its equipment and formation rates while reducing only seals.
    public static int GetComputerItemOutcome(int roll)
    {
        roll = Math.Max(0, Math.Min(99, roll));
        if (roll < 33) return 0;
        if (roll < 41) return 1;
        if (roll < 67) return -1;
        return 2;
    }

    public static int GetMissingLearners(int king, int arms)
    {
        Informations info = Informations.Instance;
        if (king < 0 || king >= info.kingNum) return 0;
        int count = 0;
        for (int i = 0; i < info.generalNum; i++)
        {
            GeneralInfo general = info.GetGeneralInfo(i);
            if (general == null || general.king != king || general.prisonerIdx != -1) continue;
            int known = TroopRules.NormalizeArms(general.arms | general.armsCur);
            if ((known & arms) == 0) count++;
        }
        return count;
    }

    public static int GetFactionSealStock(int king, int arms)
    {
        Informations info = Informations.Instance;
        int count = 0;
        for (int i = 0; i < info.cityNum; i++)
        {
            CityInfo city = info.GetCityInfo(i);
            if (city == null || city.king != king || city.objects == null) continue;
            for (int j = 0; j < city.objects.Count; j++)
            {
                int item = city.objects[j];
                if ((item >> 16) == 2 && (TroopRules.NormalizeItem(item) & 0xffff) == arms) count++;
            }
        }
        return count;
    }

    public static int GetReserveLimit(int missingLearners)
    {
        return missingLearners <= 0 ? 0 : Math.Min(3, (missingLearners + 5) / 6);
    }

    public static int ChooseSeal(int cityIndex, int choice)
    {
        Informations info = Informations.Instance;
        if (cityIndex < 0 || cityIndex >= info.cityNum) return 0;
        CityInfo city = info.GetCityInfo(cityIndex);
        if (city == null || city.king < 0 || city.king >= info.kingNum || city.objects == null || city.objects.Count >= 50) return 0;
        // Iterate two times instead of allocating arrays in this frequent path.
        int candidates = 0;
        for (int i = 0; i < 3; i++)
        {
            int arms = TroopRules.GetRecruitableArms(i);
            if (GetFactionSealStock(city.king, arms) < GetReserveLimit(GetMissingLearners(city.king, arms))) candidates++;
        }
        if (candidates == 0) return 0;
        int selected = Math.Max(0, choice) % candidates;
        for (int i = 0; i < 3; i++)
        {
            int arms = TroopRules.GetRecruitableArms(i);
            if (GetFactionSealStock(city.king, arms) >= GetReserveLimit(GetMissingLearners(city.king, arms))) continue;
            if (selected-- == 0) return arms;
        }
        return 0;
    }
}
