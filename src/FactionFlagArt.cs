using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;

// New four-frame surname sprites share one texture and retain the existing
// animation controller, sprite footprint, collider, pause and resume behavior.
public static class FactionFlagArt
{
    private static exAtlas atlas;
    private static XmlDocument definitions;
    private static Dictionary<int, exSpriteAnimClip> clips = new Dictionary<int, exSpriteAnimClip>();

    public static void EnsureAnimation(exSpriteAnimation animation, int generalIndex)
    {
        string name = "RulerFlag" + generalIndex;
        if (animation.GetAnimation(name) != null) return;
        if (atlas == null) LoadAtlas(animation);
        exSpriteAnimClip clip;
        if (!clips.TryGetValue(generalIndex, out clip))
        {
            XmlElement ruler = (XmlElement)definitions.DocumentElement.SelectSingleNode("Ruler[@generalIdx='" + generalIndex + "']");
            if (ruler == null) throw new InvalidOperationException("Missing ruler flag " + generalIndex);
            clip = ScriptableObject.CreateInstance<exSpriteAnimClip>();
            clip.name = name;
            clip.wrapMode = WrapMode.Loop;
            clip.length = 4f / 6f;
            clip.sampleRate = 6f;
            for (int i = 0; i < 4; i++)
            {
                exSpriteAnimClip.FrameInfo frame = new exSpriteAnimClip.FrameInfo();
                frame.atlas = atlas;
                frame.index = generalIndex * 4 + i;
                frame.length = 1f / 6f;
                clip.frameInfos.Add(frame);
            }
            clips.Add(generalIndex, clip);
        }
        animation.AddAnimation(name, clip);
    }

    private static void LoadAtlas(exSpriteAnimation animation)
    {
        exSpriteAnimState template = animation.GetAnimation("Flag1");
        if (template == null || template.clip.frameInfos.Count == 0)
            throw new InvalidOperationException("Original flag animation template is missing.");
        definitions = new XmlDocument();
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scenarios.RulerFlags.xml"))
        {
            if (stream == null) throw new InvalidOperationException("Ruler flag metadata is missing.");
            definitions.Load(stream);
        }
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scenarios.RulerFlags.png"))
        {
            if (stream == null) throw new InvalidOperationException("Ruler flag texture is missing.");
            using (BinaryReader reader = new BinaryReader(stream))
                if (!texture.LoadImage(reader.ReadBytes((int)stream.Length)))
                    throw new InvalidOperationException("Cannot decode ruler flag texture.");
        }
        int width = int.Parse(definitions.DocumentElement.GetAttribute("width"));
        int height = int.Parse(definitions.DocumentElement.GetAttribute("height"));
        if (texture.width != width || texture.height != height)
            throw new InvalidOperationException("Ruler flag texture dimensions do not match metadata.");
        texture.name = "RulerFlags";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Material material = new Material(template.clip.frameInfos[0].atlas.material);
        material.name = "RulerFlagsMaterial";
        material.mainTexture = texture;
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
        exAtlas newAtlas = ScriptableObject.CreateInstance<exAtlas>();
        newAtlas.name = "RulerFlagsAtlas";
        newAtlas.texture = texture;
        newAtlas.material = material;
        newAtlas.elements = BuildElements(width, height);
        atlas = newAtlas;
    }

    private static exAtlas.Element[] BuildElements(int width, int height)
    {
        exAtlas.Element[] elements = new exAtlas.Element[Informations.Instance.generalNum * 4];
        foreach (XmlElement ruler in definitions.DocumentElement.SelectNodes("Ruler"))
        {
            int general = int.Parse(ruler.GetAttribute("generalIdx"));
            for (int i = 0; i < 4; i++)
            {
                XmlElement frame = (XmlElement)ruler.ChildNodes[i];
                exAtlas.Element element = new exAtlas.Element();
                element.name = "Ruler" + general + "Frame" + i;
                element.originalWidth = int.Parse(frame.GetAttribute("originalWidth"));
                element.originalHeight = int.Parse(frame.GetAttribute("originalHeight"));
                int x = int.Parse(frame.GetAttribute("x"));
                int y = int.Parse(frame.GetAttribute("y"));
                int w = int.Parse(frame.GetAttribute("width"));
                int h = int.Parse(frame.GetAttribute("height"));
                element.trimRect = new Rect(int.Parse(frame.GetAttribute("trimX")), int.Parse(frame.GetAttribute("trimY")), w, h);
                element.coords = new Rect((float)x / width, 1f - (float)(y + h) / height, (float)w / width, (float)h / height);
                elements[general * 4 + i] = element;
            }
        }
        return elements;
    }
}
