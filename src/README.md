# 新增辅助类

这里仅收录本项目新写的辅助类，供审阅逻辑：

| 文件 | 职责 |
| --- | --- |
| [ScenarioCatalog.cs](ScenarioCatalog.cs) | 读取新增剧本目录、年份、势力数量和嵌入的剧本资源，保留原有时期编号。 |
| [ScenarioMenuFont.cs](ScenarioMenuFont.cs) | 从独立 PNG／XML 资源创建新增时期菜单字体，保留其他界面的原字体。 |
| [HistoricalRecruitmentRules.cs](HistoricalRecruitmentRules.cs) | 根据年份、地区及仍存续的历史归属势力判断在野人物能否被搜索。 |

这些类依赖原游戏的 `Controller`、`Informations`、城市／势力／武将类型，以及 Unity 和原字体组件接口。这里没有原游戏类的定义，也没有全量反编译源码；因此该目录不能单独编译成可运行游戏。

配置与资源生成方法见 [scripts/README.md](../scripts/README.md)，搜将行为说明见[历史搜将规则](../docs/RECRUITMENT.md)。

v1.3.0 另外公开以下规则代码供审阅：

| 文件 | 职责 |
| --- | --- |
| [TroopRules.cs](TroopRules.cs) | 三兵种克制、旧兵种与兵符转换、兵力合并。 |
| [CombatRules.cs](CombatRules.cs) / [AutoBattleSkill.cs](AutoBattleSkill.cs) | 智力冷却、技能效果与使用时机评分。 |
| [CoordinatedSiegeRules.cs](CoordinatedSiegeRules.cs) | 多队战力预算与真实在途援军检查。 |
| [StrategicBattleRules.cs](StrategicBattleRules.cs) | 将电脑内战连接到共用的自动战斗模拟。 |
| [FactionFlagRules.cs](FactionFlagRules.cs) | 新增君主旗帜映射，保留已有动画素材。 |

这些是辅助类，并非完整的战斗实现；对原游戏控制器的方法修改没有作为全量反编译源码发布。

v1.4.0 新增辅助规则：

| 文件 | 职责 |
| --- | --- |
| [SearchRewardRules.cs](SearchRewardRules.cs) | 搜索分支概率、全势力兵符学习需求与备用库存。 |
| [OffensiveCampaignRules.cs](OffensiveCampaignRules.cs) | 持久进攻目标、友城路径与真实在途增援。 |
| [RulerIdentityRules.cs](RulerIdentityRules.cs) | 当前君主姓名和头像资源。 |
| [FactionFlagArt.cs](FactionFlagArt.cs) / [FactionFlagRules.cs](FactionFlagRules.cs) | 君主姓氏旗帜图集、四帧动画与原旗帜选择。 |

完整进攻调度仍位于游戏控制器中；此处是可审阅的辅助规则，不是可单独编译的完整游戏源码。
