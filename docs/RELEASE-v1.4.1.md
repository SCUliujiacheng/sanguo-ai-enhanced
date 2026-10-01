# v1.4.1 自动战斗结算修正版（测试版）

- **修复技能没加载**：从启动游戏直接使用自动战斗，也会读取已学习的技能；避免先进入过手动战场与否改变自动结算。
- **修复超时硬判失败**：99 秒双方仍存活按和局处理，保留实际剩余生命、兵力和技力，不因和局清兵、俘虏或发胜利经验。
- **全体自动与电脑同步处理和局**：玩家暂停连续结算并返回选将；电脑结束本次交战并退向友城。保留此前已完成对决的俘虏结算，无路可退时避免原地重复触发。

继续保留 v1.4.0 的跨月聚兵、多队攻城、兵符按需减量、君主旗帜与头像修复，以及三兵种、智将冷却、技能名升迁、三项自动化、14 时期与历史搜将。本次没有调整伤害与克制数值。

在 Assets 下载 **sanguo-ai-enhanced-v1.4.1.apk**；安装详情为 **1.4.1 / versionCode 5**。沿用本项目 1.2.0–1.4.0 的修改版签名，覆盖更新前备份重要存档。

最终程序集 **248 项行为回归通过**，另有 **414 万次受控单挑统计**；APK 签名、对齐和载荷校验通过。尚未完成本版 Android 实机验收，标记为 **Pre-release 测试版**。自动结算仍是简化模拟，不保证高武力在所有条件下获胜。

[玩法说明](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/blob/main/docs/GAMEPLAY-v1.4.1.md) · [验证报告](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/blob/main/docs/VALIDATION-v1.4.1.md) · [安装说明](https://github.com/SCUliujiacheng/sanguo-ai-enhanced/blob/main/docs/INSTALL.md)

SHA-256：`3f1e1a2ba0f31f008599fcd947aa91a8d11a2b8be8023493b14a74dbd0493302`
