# ADR 0030：G3.3 正式 TMP 字体、本地化表路由与运行时边界

- 状态：Accepted
- 日期：2026-08-10
- 决策人：依据用户连续完成 Demo、全权自行决策与逐步骤提交 Push 授权
- 关联里程碑：G3.3、M13
- 承接：ADR 0009、0010、0026、0028、0029

## 背景

FONT-001、FONT-002 和三批双语 String Table 已分别通过资产门禁，但 G2.6/G2.7 运行时仍使用
`UnityEngine.UI.Text`、`LegacyRuntime.ttf` 或操作系统动态字体，单一 UI 表适配器也不能解析内容与叙事
命名空间。该状态无法保证 Player 与 Editor 字形一致，伪本地化字符可能触发运行时扩图，伤害数字则会
绕过正式字体和 provenance。

## 决策

- `Game.UI` 的页面、HUD、危险提示以及 `Game.Presentation` 的池化伤害数字统一使用 TextMeshPro。
  禁止 `Font.CreateDynamicFontFromOSFont`、`LegacyRuntime.ttf` 和运行时系统字体回退。
- Noto Sans CJK SC Regular 为 TMP 默认与正文/HUD 字体，Bold 用于危险提示，Noto Serif CJK SC
  SemiBold 用于标题、故事和结算。三者由 `QinglanFormalFontLoader` 在 Infrastructure 启动点通过
  Addressables 加载并唯一持有 Handle。
- `UnityLocalizationService` 根据稳定 Key 路由：`ui.*` 进入 `UI`，`content.*` 进入
  `QinglanContent`，`story.*`、`collectible.*` 与 `narrative.*` 进入 `QinglanNarrative`。
- English 与 zh-Hans 的三张表均预加载；Pseudo 以 English 为源，ASCII 映射为 Noto CJK 可覆盖的
  全角字符，并使用 `【】` 包围。正式 UI 不把伪本地化结果写入存档或内容定义。
- Editor 集成器从六张正式表、Pseudo 结果与固定运行时符号收集实际字符，预热三套 Dynamic、
  Multi Atlas TMP 资产。治理验证逐字检查持久化 Glyph Table，并校验字体与表 provenance。
- 1920×1080 下覆盖 en、zh-Hans、Pseudo 与 100%/125%/150% 字号；正式 Player 冒烟必须证明字体
  Addressables、三表解析、Locale 循环、关键字形和 150% 布局均正常。

## 依赖方向

```text
Localization Tables ---> Game.UI adapter ---> TMP page/HUD
Formal TMP Addressables -> Game.Infrastructure -> Game.UI
                                           \---> Game.Presentation damage-number pool
```

`Game.UI`、`Game.Presentation`、`Game.Infrastructure` 增加 `Unity.TextMeshPro` 直接 Package 依赖；
`Game.Editor` 与测试程序集增加同依赖用于生成和验证。产品程序集之间不新增反向边，`Game.Core` 与
`Game.Simulation` 继续不引用 UnityEngine，Content Schema 6、存档 Schema 3 和 30 Hz Tick 不变。

## 兼容、迁移与回滚

既有稳定本地化 Key、Presenter/ViewModel 和纯值 HUD 投影不变。Development 在正式字体加载失败时仅可
回退到仓库治理的 TMP 默认字体并输出警告；Release 由验证器阻断缺字体、缺字形、缺表或 provenance
不一致。回滚可撤销 Loader/TMP View，但不得恢复操作系统或 Legacy 字体作为发布路径。

## 测试

EditMode 覆盖三表路由、六表预加载、Pseudo 方法、全字形与程序集白名单；PlayMode 覆盖实际
Addressables 加载、三 Locale 三表解析、全 TMP 文本树、Serif 路由和 150% 无溢出。最终执行全量
EditMode/PlayMode、Project Validation、Windows Development Build 和构建后 Player Smoke。
