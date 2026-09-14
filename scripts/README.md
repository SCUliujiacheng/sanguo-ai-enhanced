# 配置与资源生成工具

这些脚本用于编辑、检查和生成新增剧本、历史搜将规则及菜单字体。**它们不是完整 APK 的一键编译工具。** 运行脚本不会修改已经安装的游戏，也不会把生成文件自动注入 APK。

以下命令从仓库根目录执行。剧本与搜将生成器只依赖 Python 3 标准库；菜单字体另需 Pillow、fonttools 和本地下载的 Noto 字体。

## 生成新增剧本

脚本：[generate_scenarios.py](generate_scenarios.py)。默认读取仓库的 [scenarios](../scenarios) 目录，也可用 `--config-dir` 指定另一个配置目录。

先在本地准备从同一基础版本游戏中提取的输入文件，保持原有编号、结构和顺序：

```text
local/base/
  MOD01.xml
  MOD02.xml
  MOD03.xml
  MOD04.xml
  MOD05.xml
  names.json
```

`names.json` 需要包含 `cityName` 和 `generalName` 两个数组，分别对应原游戏顺序中的 48 座城池与 255 名武将。请勿仅按文字名称重排数组；剧本会用这些下标关联原游戏数据。

这些输入模板和游戏资源不随公开仓库提供。生成器不会代替资源提取步骤，也不适用于城池数、武将数或 XML 结构不同的游戏版本。

```sh
python -X utf8 scripts/generate_scenarios.py --base-dir local/base --output-dir build/scenarios
```

脚本读取 `06-*.json` 至 `14-*.json` 的配置，按配置中的 `template` 加载本地模板，输出 `MOD06.xml` 至 `MOD14.xml` 及 `scenario-report.json`。报告包括年份、势力、城市与武将分配、可用武将数和生成文件 SHA-256。

可修改的主要配置：

- `name`、`year`、`kind`：菜单名称、开局年份和历史／架空类型。
- `factions`：君主、首府、领城、武将名单、前线和固定驻将。
- `freeGenerals`：开局可用的在野武将。
- `economy` 与势力内的资源覆盖项：开局资金、城防和预备兵。
- [roster_bounds.json](../scenarios/roster_bounds.json)：历史开局中人物的可用年份边界。

生成器会检查重复分配、编号与归属、城池容量、武将状态和基础资源范围。配置检查通过表示数据符合这些规则，并不等于该剧本已经完成手机实玩或难度平衡测试。

## 生成历史搜将规则

脚本：[generate_recruitment.py](generate_recruitment.py)。此脚本直接读取 [historical_recruitment.json](../scenarios/historical_recruitment.json)，不需要原剧本模板：

```sh
python -X utf8 scripts/generate_recruitment.py --output build/scenarios/HistoricalRecruitment.xml
```

如需读取自行编辑的其他规则文件，使用 `--config path/to/rules.json`。每名武将记录包含编号、名称、证据说明和若干年份阶段；阶段规定可搜索年份、地区与历史归属君主编号。

脚本检查原有 255 名武将的编号覆盖、年份阶段是否重叠，以及城市和君主编号是否有效。它不会验证历史考证的准确性。带有“推定”说明的条目应继续保留说明，并在有可靠资料时逐项修订。

玩法、历史势力灭亡后的处理和规则边界见[历史搜将规则](../docs/RECRUITMENT.md)。

## 生成菜单字体

脚本：[build_menu_font.py](build_menu_font.py)。完整输入、命令和字体许可见[菜单字体生成](../docs/MENU_FONT.md)。请将生成的 `FONT-NOTICE.txt` 与 `OFL.txt` 和字体资源一起保留。

## 查看新增运行时类

[src](../src) 仅提供本项目新写的三个辅助类，供审阅剧本读取、菜单字体与搜将规则。它们依赖原游戏和 Unity 的类型，不构成可独立编译的完整工程。

完整 APK 还需要与对应游戏版本匹配的运行时补丁、程序集集成、资源嵌入、签名与设备验证。公开脚本目前只负责上述数据和字形生成；生成的 XML 或 PNG 不能直接安装。普通玩家请下载 [Releases](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/releases) 中的 APK。
