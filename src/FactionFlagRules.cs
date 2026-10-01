using UnityEngine;

// Original named rulers retain their verified legacy surname banner. A ruler
// without a matching legacy banner gets their actual surname's new animation.
public static class FactionFlagRules
{
    public static int GetPreferredFlag(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        string[] originals = ZhongWen.Instance.kingNames;
        for (int i = 0; i < originals.Length; i++)
            if (name == originals[i] && i != 18) return i + 1;
        switch (name)
        {
            case "马超": return 11; // Ma Teng's Ma banner
            case "曹丕": case "曹叡": return 1;
            case "孙策": return 3;
            case "公孙康": return 7;
            default: return 0;
        }
    }

    public static int GetFlagIndex(int kingIndex)
    {
        if (kingIndex == Informations.Instance.kingNum) return 19;
        int general = RulerIdentityRules.GetGeneralIndex(kingIndex);
        if (general < 0) return 0;
        int legacy = GetPreferredFlag(RulerIdentityRules.GetName(kingIndex));
        return legacy > 0 ? legacy : 31 + general;
    }

    public static string GetAnimationName(int kingIndex)
    {
        int flag = GetFlagIndex(kingIndex);
        if (flag == 0) return string.Empty;
        return flag <= 30 ? "Flag" + flag : "RulerFlag" + (flag - 31);
    }

    public static void Apply(exSpriteAnimation animation, int kingIndex)
    {
        if (animation == null) return;
        string name = GetAnimationName(kingIndex);
        Renderer renderer = animation.GetComponent<Renderer>();
        if (name.Length == 0) { renderer.enabled = false; return; }
        if (GetFlagIndex(kingIndex) > 30)
            FactionFlagArt.EnsureAnimation(animation, RulerIdentityRules.GetGeneralIndex(kingIndex));
        animation.Play(name);
        renderer.enabled = true;
    }
}
