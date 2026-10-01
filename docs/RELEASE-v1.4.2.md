# v1.4.2 技能条显示修正版（测试版）

- **修复施法后红色技能条消失**：提前更新手动战斗左右剑形条的裁剪与显隐，避免渲染阶段启用 Renderer 的错误。
- **保留充能与技能规则**：充能速度、技力消耗、伤害与自动结算不变。手机诊断中夏侯惇技力 51 → 29 → 7，已实际施法两次；异常在画面显示。
- 新版去掉诊断覆盖面板，继续保留自动内政、自动战斗、装备自动分配、三兵种、14 时期、历史搜将与多队攻城等既有功能。

在 Assets 下载 **sanguo-ai-enhanced-v1.4.2.apk**；安装详情为 **1.4.2 / versionCode 6**。沿用本项目修改版签名，覆盖更新前备份重要存档。

本轮 **67 项行为回归通过**，新增渲染阶段测试先让旧版失败，再确认新版通过。APK 签名、对齐、CRC 和载荷校验通过。**尚未完成新版 Android 画面实机验收**，发布为 Pre-release 测试版；请复测首次及再次施法后左右红色剑形条的增长和菜单返回后的显示。

[玩法说明](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/blob/main/docs/GAMEPLAY-v1.4.2.md) · [验证报告](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/blob/main/docs/VALIDATION-v1.4.2.md) · [安装与存档](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/blob/main/docs/INSTALL.md)

SHA-256：`9d01877389d7788a60b6be610ced7221515ccc17190763f7aebc935e1e89c169`
