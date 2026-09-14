# 新增辅助类

这里仅收录本项目新写的三个类，供审阅逻辑：

| 文件 | 职责 |
| --- | --- |
| [ScenarioCatalog.cs](ScenarioCatalog.cs) | 读取新增剧本目录、年份、势力数量和嵌入的剧本资源，保留原有时期编号。 |
| [ScenarioMenuFont.cs](ScenarioMenuFont.cs) | 从独立 PNG／XML 资源创建新增时期菜单字体，保留其他界面的原字体。 |
| [HistoricalRecruitmentRules.cs](HistoricalRecruitmentRules.cs) | 根据年份、地区及仍存续的历史归属势力判断在野人物能否被搜索。 |

这些类依赖原游戏的 `Controller`、`Informations`、城市／势力／武将类型，以及 Unity 和原字体组件接口。这里没有原游戏类的定义，也没有全量反编译源码；因此该目录不能单独编译成可运行游戏。

配置与资源生成方法见 [scripts/README.md](../scripts/README.md)，搜将行为说明见[历史搜将规则](../docs/RECRUITMENT.md)。
