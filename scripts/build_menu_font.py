#!/usr/bin/env python3
"""Build a separate single-page bitmap font for the expanded scenario menu.

Requires Pillow and fonttools. The original game atlas is never read or changed.
Pass an official Noto Serif CJK SC font and its OFL license; font binaries are
local build inputs and are intentionally not copied into the public repository.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET

from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont


TITLES = [
    "黄巾之乱", "讨伐董卓", "群雄割据", "赤壁之战", "三国鼎立",
    "官渡争锋", "三顾茅庐", "潼关风云", "入主巴蜀", "汉中争夺",
    "夷陵之战", "北伐中原", "英雄集结", "孤城逆袭",
]
MENU_TEXTS = TITLES + [
    "上一页 下一页 0123456789()/（）【】 - 年 历史 架空 挑战",
    "选择时期 返回 選擇時期 黃巾之亂 討伐董卓 群雄割據 赤壁之戰 三國鼎立",
]
SIZE = 22
LINE_HEIGHT = 26
BASELINE = 21
ATLAS_WIDTH = 256
PADDING = 2


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def draw_atlas_text(target: Image.Image, text: str, x: int, y: int,
                    atlas: Image.Image, glyphs: dict[int, dict], color=(242, 234, 214)) -> int:
    """Render through generated glyph metrics, not the source font renderer."""
    cursor = x
    for char in text:
        glyph = glyphs[ord(char)]
        if glyph["width"] and glyph["height"]:
            patch = atlas.crop((glyph["x"], glyph["y"],
                                glyph["x"] + glyph["width"],
                                glyph["y"] + glyph["height"]))
            ink = Image.new("RGBA", patch.size, (*color, 255))
            ink.putalpha(patch.getchannel("A"))
            target.alpha_composite(ink, (cursor + glyph["xoffset"], y + glyph["yoffset"]))
        cursor += glyph["xadvance"]
    return cursor - x


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--font", required=True, type=Path)
    parser.add_argument("--license", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--source-url", default="https://github.com/notofonts/noto-cjk")
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)

    font = ImageFont.truetype(str(args.font), SIZE)
    chars = sorted(set("".join(MENU_TEXTS)), key=ord)
    with TTFont(args.font) as source:
        cmap = source.getBestCmap()
        missing = [char for char in chars if ord(char) not in cmap]
        copyright_notice = source["name"].getDebugName(0) or "See source font copyright notice."
        font_version = source["name"].getDebugName(5) or "Unknown version"
    if missing:
        raise ValueError("Source font lacks required characters: " + "".join(missing))

    glyphs = []
    bitmaps = []
    pen_x = PADDING
    pen_y = PADDING
    row_height = 0
    for char in chars:
        left, top, right, bottom = font.getbbox(char, anchor="ls")
        width, height = right - left, bottom - top
        advance = max(1, round(font.getlength(char)))
        glyph = {"id": ord(char), "x": 0, "y": 0, "width": width,
                 "height": height, "xoffset": left,
                 "yoffset": BASELINE + top, "xadvance": advance}
        if char == " ":
            glyph.update(width=0, height=0, xoffset=0, yoffset=0)
            glyphs.append(glyph)
            bitmaps.append(None)
            continue
        if glyph["yoffset"] < 0 or glyph["yoffset"] + height > LINE_HEIGHT:
            raise ValueError(f"Glyph {char!r} does not fit lineHeight {LINE_HEIGHT}: {glyph}")
        if pen_x + width + PADDING > ATLAS_WIDTH:
            pen_x = PADDING
            pen_y += row_height + PADDING
            row_height = 0
        glyph["x"], glyph["y"] = pen_x, pen_y
        mask = Image.new("L", (width, height), 0)
        ImageDraw.Draw(mask).text((-left, -top), char, font=font, anchor="ls", fill=255)
        if not mask.getbbox():
            raise ValueError(f"Empty visible glyph {char!r}")
        glyphs.append(glyph)
        bitmaps.append(mask)
        pen_x += width + PADDING
        row_height = max(row_height, height)

    required_height = pen_y + row_height + PADDING
    atlas_height = 2 ** math.ceil(math.log2(required_height))
    atlas = Image.new("RGBA", (ATLAS_WIDTH, atlas_height), (255, 255, 255, 0))
    for glyph, mask in zip(glyphs, bitmaps):
        if mask is not None:
            patch = Image.new("RGBA", mask.size, (255, 255, 255, 255))
            patch.putalpha(mask)
            atlas.paste(patch, (glyph["x"], glyph["y"]))
    atlas.save(args.out / "MenuFont.png")

    root = ET.Element("Font", width=str(ATLAS_WIDTH), height=str(atlas_height),
                      lineHeight=str(LINE_HEIGHT), size=str(SIZE))
    for glyph in glyphs:
        ET.SubElement(root, "Glyph", **{key: str(value) for key, value in glyph.items()})
    ET.indent(root, space="  ")
    ET.ElementTree(root).write(args.out / "MenuFont.xml", encoding="utf-8", xml_declaration=True)
    metadata = {
        "font": "Noto Serif CJK SC Regular", "source_url": args.source_url,
        "copyright": copyright_notice, "font_version": font_version,
        "source_font_sha256": sha256(args.font), "license_sha256": sha256(args.license),
        "license": "OFL.txt", "size": SIZE, "lineHeight": LINE_HEIGHT,
        "baseline": BASELINE, "width": ATLAS_WIDTH, "height": atlas_height,
        "glyph_count": len(glyphs), "characters": "".join(chars), "glyphs": glyphs,
        "image_sha256": sha256(args.out / "MenuFont.png"),
        "metrics_sha256": sha256(args.out / "MenuFont.xml"),
    }
    (args.out / "MenuFont.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    shutil.copyfile(args.license, args.out / "OFL.txt")
    (args.out / "FONT-NOTICE.txt").write_text(
        "Scenario Menu Font\nGenerated from Noto Serif CJK SC Regular\n"
        + copyright_notice + "\n" + font_version + "\n"
        + "Source: " + args.source_url + "\n"
        + "Licensed under the SIL Open Font License, Version 1.1. See OFL.txt.\n",
        encoding="utf-8")

    glyph_map = {glyph["id"]: glyph for glyph in glyphs}
    preview = Image.new("RGBA", (960, 390), (22, 21, 34, 255))
    draw = ImageDraw.Draw(preview)
    for page in range(3):
        x = 20 + page * 320
        draw.rounded_rectangle((x, 16, x + 299, 372), radius=5,
                               fill=(12, 17, 39, 255), outline=(155, 137, 84, 255), width=2)
        draw_atlas_text(preview, "选择时期", x + 98, 35, atlas, glyph_map, (235, 209, 91))
        for row, title in enumerate(TITLES[page * 5:(page + 1) * 5]):
            text_x = x + (300 - sum(glyph_map[ord(c)]["xadvance"] for c in title)) // 2
            draw_atlas_text(preview, title, text_x, 91 + row * 40, atlas, glyph_map)
        draw_atlas_text(preview, "上一页", x + 17, 330, atlas, glyph_map)
        draw_atlas_text(preview, f"{page + 1}/3", x + 131, 330, atlas, glyph_map, (235, 209, 91))
        draw_atlas_text(preview, "下一页", x + 217, 330, atlas, glyph_map)
    preview.convert("RGB").save(args.out / "MenuFont-preview.png")
    print(json.dumps({"glyph_count": len(glyphs), "atlas": [ATLAS_WIDTH, atlas_height],
                      "size": SIZE, "lineHeight": LINE_HEIGHT, "all_required_glyphs_present": True,
                      "output_files": ["MenuFont.png", "MenuFont.xml", "MenuFont.json", "OFL.txt", "FONT-NOTICE.txt", "MenuFont-preview.png"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
