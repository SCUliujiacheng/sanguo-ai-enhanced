using System;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;

// The original five save-game IDs never change. Expanded definitions are
// embedded into Assembly-CSharp.dll so an APK needs no additional loose files.
public static class ScenarioCatalog
{
    public const int OriginalCount = 5;
    public const int Count = 14;
    private static XmlDocument[] documents = new XmlDocument[Count];
    private static bool[][] availableGenerals = new bool[Count][];

    public static bool IsValidIndex(int index)
    {
        return index >= 0 && index < Count;
    }

    public static string GetName(int index)
    {
        switch (index)
        {
            case 0: return "黄巾之乱";
            case 1: return "讨伐董卓";
            case 2: return "群雄割据";
            case 3: return "赤壁之战";
            case 4: return "三国鼎立";
            case 12:
            case 13: return LoadDocument(index).DocumentElement.GetAttribute("Name") + "(架空)";
            default: return LoadDocument(index).DocumentElement.GetAttribute("Name");
        }
    }

    public static int GetYear(int index)
    {
        switch (index)
        {
            case 0: return 184;
            case 1: return 190;
            case 2: return 200;
            case 3: return 208;
            case 4: return 219;
            default: return int.Parse(LoadDocument(index).DocumentElement.GetAttribute("Year"));
        }
    }

    public static int GetKingCount(int index)
    {
        switch (index)
        {
            case 0: return 14;
            case 1: return 18;
            case 2: return 8;
            case 3: return 8;
            case 4: return 5;
            default: return LoadDocument(index).DocumentElement.SelectSingleNode("King").ChildNodes.Count;
        }
    }

    public static int GetSelectableCount(int index)
    {
        switch (index)
        {
            case 0: return 6;
            case 1: return 7;
            case 2: return 5;
            case 3: return 5;
            case 4: return 5;
            default:
                int count = int.Parse(LoadDocument(index).DocumentElement.GetAttribute("SelectableCount"));
                if (count < 1 || count > GetKingCount(index))
                    throw new InvalidOperationException("Invalid selectable faction count for scenario " + index);
                return count;
        }
    }

    public static string GetExpandedKingName(int index, int kingIndex)
    {
        if (index < OriginalCount || !IsValidIndex(index) || kingIndex < 0 || kingIndex >= GetKingCount(index))
            return string.Empty;
        XmlElement king = (XmlElement)LoadDocument(index).DocumentElement.SelectSingleNode("King").ChildNodes[kingIndex];
        if (king.HasAttribute("Name")) return king.GetAttribute("Name");
        return ZhongWen.Instance.GetGeneralName(int.Parse(king.GetAttribute("GeneralIdx")));
    }

    public static void ConfigureInformations(Informations information, int index)
    {
        if (!IsValidIndex(index)) throw new InvalidOperationException("Unknown scenario index " + index);
        int[] counts = new int[Count];
        for (int i = 0; i < Count; i++) counts[i] = GetKingCount(i);
        information.modKingNum = counts;
        information.kingNum = counts[index];
    }

    public static bool IsGeneralAvailable(int generalIndex)
    {
        int index = Controller.MODSelect;
        if (index >= 0 && index < OriginalCount) return true;
        if (!IsValidIndex(index) || generalIndex < 0) return false;
        if (availableGenerals[index] == null)
        {
            XmlNodeList generals = LoadDocument(index).DocumentElement.SelectSingleNode("General").ChildNodes;
            bool[] available = new bool[generals.Count];
            for (int i = 0; i < generals.Count; i++)
                available[i] = ((XmlElement)generals[i]).GetAttribute("Available") != "0";
            availableGenerals[index] = available;
        }
        return generalIndex < availableGenerals[index].Length && availableGenerals[index][generalIndex];
    }

    public static XmlDocument LoadDocument(int index)
    {
        if (!IsValidIndex(index)) throw new InvalidOperationException("Unknown scenario index " + index);
        if (documents[index] != null) return documents[index];
        string assetName = "MOD" + (index + 1).ToString("D2");
        XmlDocument document = new XmlDocument();
        if (index < OriginalCount)
        {
            TextAsset textAsset = (TextAsset)Resources.Load(assetName);
            if (textAsset == null) throw new InvalidOperationException("Missing scenario asset " + assetName);
            document.LoadXml(textAsset.text.Trim());
        }
        else
        {
            string resourceName = "Scenarios." + assetName + ".xml";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException("Missing embedded scenario " + resourceName);
                document.Load(stream);
            }
            XmlElement root = document.DocumentElement;
            if (root == null || !root.HasAttribute("Name") || !root.HasAttribute("Year") ||
                !root.HasAttribute("SelectableCount") || root.SelectSingleNode("King") == null ||
                root.SelectSingleNode("City") == null || root.SelectSingleNode("General") == null)
                throw new InvalidOperationException("Incomplete scenario definition " + resourceName);
        }
        documents[index] = document;
        return document;
    }
}
