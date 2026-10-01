# v1.4.2 验证报告

验证日期：2026-10-01。本轮仅修复手动战斗左右剑形技能条的显示时序。以下测试与 APK 对应同一个最终 DLL；玩家诊断截图确认的是旧版故障，**新版 Android 画面仍待实机确认**。

## 来自手机的故障证据

| 倒计时 | 夏侯惇技力 | 内部充能 / 132 | 原剑形条与错误 |
| ---: | ---: | ---: | --- |
| 89 | 51 / 51 | 132.0 | 红色进度可见，尚无记录错误 |
| 73 | 29 / 51 | 106.1 | 红色进度消失，记录到 Renderer 渲染阶段错误 |
| 67 | 7 / 51 | 10.7 | 已再次施法并重新充能，红色进度仍消失 |

诊断记录 `Enabling or adding a Renderer during rendering: this is not allowed. Renderer`。同时 sprite/Renderer 的 enabled getter、相机和纹理仍有有效值；充能与网格宽度正常变化。不能把 getter 为真等同于手机画面已正确提交，也不能据此推断 Unity 的某个私有内部字段值。

## 修复及原有逻辑保持情况

仅新增 `WSInfoPanel.LateUpdate` 与 `RefreshSkillMeterClipping`，对存在且启用的左右剑形条执行 `Commit` 和 `UpdateClipInfo`，提前完成零宽隐藏及正宽恢复。原 ex2D 管理器继续提交网格，不修改 ex2D DLL。没有新增诊断字段、日志订阅或 OnGUI 面板。

Unity 5.6 的执行顺序将 LateUpdate 放在 Update 之后、渲染之前；此处将需要重新启用 Renderer 的操作移出原 OnPreRender 回调。官方资料支持阶段选择，手机原生错误提供故障证据。[Unity 5.6 执行顺序](https://docs.unity3d.com/560/Documentation/Manual/ExecutionOrder.html)、[LateUpdate](https://docs.unity3d.com/560/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html)、[OnPreRender](https://docs.unity3d.com/560/Documentation/ScriptReference/MonoBehaviour.OnPreRender.html)

冻结 v1.4.1 对比审查：**456 个原类型、3,109 个原字段、3,344 个原方法、18 个嵌入资源及依赖保持一致**。唯一增加上述两个方法，技能充能、技力扣除、伤害、兵种克制、自动结算、AI 与存档逻辑不变。CLR 2.0、成员解析与指令栈检查通过，无新增外部依赖。

## 先复现，再验证

同一测试流程执行真实游戏 Update、技能重置回调、候选 LateUpdate（若存在）及真实 ex2D OnPreRender。原生边界模拟手机已观察到的限制：渲染过程中从关闭转为启用 Renderer 记录错误；getter 仍可为真。额外的显示提交状态是测试模型，**不是对 Unity 私有实现或 Android GPU 的测量**。

- 冻结 v1.4.1：4 项阶段测试 **3 失败、1 通过**。左右首次施法后网格都继续增长，但恢复 Renderer 发生在渲染阶段；双方连续 6 次重置共出现 12 次违规启用。
- v1.4.2：相同 4 项阶段测试 **4 / 4 通过**，连续 6 次双方重置恢复至完整 132 宽度，违规启用为 0。打开菜单暂停与恢复保持原充能速率。
- 新阶段测试包含在以下 12 项生命周期套件中，未重复计入总数。

## 本轮行为回归

| 组别 | 通过 |
| --- | ---: |
| 手动技能充能与重复施放 | 14 / 14 |
| 实际 ex2D 显隐、网格与渲染阶段回归 | 12 / 12 |
| 战斗强弱、兵种克制与技能效果 | 30 / 30 |
| 自动战斗技能加载与玩家和局 | 11 / 11 |
| **合计** | **67 / 67** |

测试保留实际 DLL 逻辑及 ex2D 裁剪、启停、顶点和 UV 生成；对 Unity 原生接口、资源与绘制边界的适配见每份 [JSON 报告](validation-v1.4.2/)。充能测试包含双侧多次施法、零技力、智力加速、菜单暂停及马岱受击/撤退状态。战斗和自动输入回归确认既有技能与和局结果未变。

[v1.4.1 的 248 项行为回归与 414 万次单挑统计](VALIDATION-v1.4.1.md)为历史记录。本轮没有重新运行那套完整矩阵，不作为 v1.4.2 新测试数量。

## APK 校验

- 包名 `com.castle6.sanguo.sangx`，versionName **1.4.2**、versionCode **6**。
- APK v1/v2 签名、ZIP 对齐、全包 CRC 与逐项内容比对通过；APK 内 DLL 哈希与测试 DLL 相同。
- 沿用修改版证书，SHA-256 为 `9e39ce1ac568d89b90c09cc54915bacd6d0100b943a6d69bc2d9bbd60e76271c`。
- 相比 v1.4.1，仅替换游戏程序集、版本清单及签名；其余 **2,519 个载荷文件**内容相同，旧安装包保留。
- APK 大小：**64,253,174 字节**。

| 文件 | SHA-256 |
| --- | --- |
| sanguo-ai-enhanced-v1.4.2.apk | `9d01877389d7788a60b6be610ced7221515ccc17190763f7aebc935e1e89c169` |
| Assembly-CSharp.dll | `a3b8d24d0335c358c6203cd3d3d4dc253bcebabf285053a8e3bb41e81d30511f` |

## 验证边界

尚未执行新版在 Android 上的安装、触摸、GPU 渲染或长局实玩。旧版的手机错误与新版的托管回归共同支持此修复，但不能替代新版手机验收。发布为 Pre-release；需确认连续施法后左右红色技能条重新出现、增长与再次归零，及菜单返回后的显示。

[玩法说明](GAMEPLAY-v1.4.2.md) · [安装与存档](INSTALL.md)
