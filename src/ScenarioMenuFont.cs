using System;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;

// A tiny, complete bitmap font for the expanded period menu. Other screens
// keep their existing fonts and materials.
public static class ScenarioMenuFont
{
    private static exBitmapFont cachedFont;

    public static exBitmapFont Get(exBitmapFont template)
    {
        if (cachedFont != null) return cachedFont;
        if (template == null || template.pageInfos.Count == 0 || template.pageInfos[0].material == null)
            throw new InvalidOperationException("The scenario menu font template is missing.");

        XmlDocument document = new XmlDocument();
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scenarios.MenuFont.xml"))
        {
            if (stream == null) throw new InvalidOperationException("Scenarios.MenuFont.xml is missing.");
            document.Load(stream);
        }
        XmlElement root = document.DocumentElement;
        int width = int.Parse(root.GetAttribute("width"));
        int height = int.Parse(root.GetAttribute("height"));
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scenarios.MenuFont.png"))
        {
            if (stream == null) throw new InvalidOperationException("Scenarios.MenuFont.png is missing.");
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (!texture.LoadImage(reader.ReadBytes((int)stream.Length)))
                    throw new InvalidOperationException("Cannot decode the scenario menu font.");
            }
        }
        if (texture.width != width || texture.height != height)
            throw new InvalidOperationException("Scenario font atlas dimensions do not match its metadata.");
        texture.name = "ScenarioMenuFontTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Material material = new Material(template.pageInfos[0].material);
        material.name = "ScenarioMenuFontMaterial";
        material.mainTexture = texture;
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;

        exBitmapFont font = ScriptableObject.CreateInstance<exBitmapFont>();
        font.name = "ScenarioMenuFont";
        font.lineHeight = int.Parse(root.GetAttribute("lineHeight"));
        font.size = int.Parse(root.GetAttribute("size"));
        exBitmapFont.PageInfo page = new exBitmapFont.PageInfo();
        page.texture = texture;
        page.material = material;
        font.pageInfos.Add(page);
        foreach (XmlElement element in root.SelectNodes("Glyph"))
        {
            exBitmapFont.CharInfo glyph = new exBitmapFont.CharInfo();
            glyph.id = int.Parse(element.GetAttribute("id"));
            glyph.x = int.Parse(element.GetAttribute("x"));
            glyph.y = int.Parse(element.GetAttribute("y"));
            glyph.width = int.Parse(element.GetAttribute("width"));
            glyph.height = int.Parse(element.GetAttribute("height"));
            glyph.xoffset = int.Parse(element.GetAttribute("xoffset"));
            glyph.yoffset = int.Parse(element.GetAttribute("yoffset"));
            glyph.xadvance = int.Parse(element.GetAttribute("xadvance"));
            glyph.page = 0;
            glyph.uv0 = new Vector2((float)glyph.x / width, 1f - (float)(glyph.y + glyph.height) / height);
            font.charInfos.Add(glyph);
        }
        font.RebuildIdToCharInfoTable();
        cachedFont = font;
        return cachedFont;
    }
}
