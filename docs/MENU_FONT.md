# 菜单字体生成

原游戏的位图字体没有包含全部新增时期所需汉字。生成脚本为时期菜单单独制作一张字体页，原字体图集保持原样。

## 构建输入

- Python 3、Pillow 和 fonttools。
- [官方 Noto Serif CJK SC Regular OTF](https://github.com/notofonts/noto-cjk/blob/f8d157532fbfaeda587e826d4cd5b21a49186f7c/Serif/OTF/SimplifiedChinese/NotoSerifCJKsc-Regular.otf)。
- 同一仓库版本的 [Serif/LICENSE](https://github.com/notofonts/noto-cjk/blob/f8d157532fbfaeda587e826d4cd5b21a49186f7c/Serif/LICENSE)。

本次使用字体的 SHA-256 为 `2a2eae2628df83556c54018c41e20fa532c1b862c5256ae8b3f23feb918d12ca`。许可证源文件 SHA-256 为 `6a73f9541c2de74158c0e7cf6b0a58ef774f5a780bf191f2d7ec9cc53efe2bf2`。

## 生成

把下载的 OTF 与许可证留在本地缓存目录，然后运行：

```sh
python scripts/build_menu_font.py --font cache/NotoSerifCJKsc-Regular.otf --license cache/OFL.txt --out build/menu-font
```

脚本生成：

- `MenuFont.png`：单页 RGBA 白色字形与透明背景。
- `MenuFont.xml`：供运行时读取的字形坐标与排版指标。
- `MenuFont.json`：字符覆盖、来源、文件校验和及相同指标。
- `MenuFont-preview.png`：使用生成的图片和指标绘制的三页文字预览。
- `FONT-NOTICE.txt` 和 `OFL.txt`：字体版权说明与许可证，须随字形一起保留。

当前尺寸为 22 像素、行高 26 像素，字图为 256 × 256 像素，覆盖 14 个时期名称、翻页文字及数字和相关符号。脚本会检查源字体缺字、空白字形和超出行高的情况。

XML 坐标以图片左上角为原点。`Glyph` 记录 Unicode 码位、图片区域、横纵偏移和前进宽度；空格只有前进宽度。预览只能用于字形和排版检查，不能代替 Android 游戏内界面测试。
