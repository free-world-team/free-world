# G3.3 正式字体与本地化

## 1. 目标

在不依赖操作系统字体的前提下，为 Demo 建立可审计、可寻址、可扫描字形的 TMP 字体与
Localization 正式内容链路，覆盖简体中文、英文和伪本地化，并验证 100% / 125% / 150%
字体缩放下的布局安全。

## 2. 执行顺序

| 顺序 | 批次 | 最低交付 | 状态 |
|---:|---|---|---|
| 1 | FONT-001 | Noto Sans CJK SC Regular / Bold 两个 TMP Font Asset | PASS（5/5 EditMode + Project Validation） |
| 2 | FONT-002 | Noto Serif CJK SC SemiBold TMP Font Asset | PASS（3/3 EditMode + Project Validation） |
| 3 | LOC-UI-001 | `UI` 集合不少于 180 个 Key | PASS（191 Key，5/5 聚焦 + 22/22 回归） |
| 4 | LOC-CONTENT-001 | `QinglanContent` 集合不少于 296 个 Key | PASS（492 Key，4/4 聚焦 + 5/5 UI 回归 + Project Validation） |
| 5 | LOC-NARRATIVE-001 | `QinglanNarrative` 集合不少于 120 个 Key | PASS（123 Key，5/5 EditMode + Project Validation） |
| 6 | G3.3 集成 | TMP 运行时、三 Locale、伪本地化、字形与布局门禁 | PASS（445/445 EditMode、20/20 PlayMode、Validation、Build、Player Smoke） |

## 3. FONT-001 设计

- 上游固定为 Noto CJK 官方仓库 `Sans2.004` 标签、提交
  `523d033d6cb47f4a80c58a35753646f5c3608a78`。
- 原始 OTF 与 OFL 1.1 许可证保存在 `Assets/ThirdParty`，逐文件记录大小与 SHA-256，
  并登记 `THIRD_PARTY_NOTICES.md`。
- 派生字体为 1024×1024、SDFAA、Dynamic、Multi Atlas TMP Font Asset；保留源字体数据，
  最终集成阶段依据三套本地化表执行完整字形扫描与预热。
- 运行时资产进入 `ThirdParty-Fonts` Addressables 组，使用 `pack.qinglan_demo`、`release`、
  `localization.release` 三个标签。
- 稳定地址为 `qinglan/font/noto-sans-cjk-sc/regular` 与
  `qinglan/font/noto-sans-cjk-sc/bold`。

## 4. 门禁

- 源文件固定版本、大小、SHA-256、许可证文本必须一致。
- TMP 资产必须引用仓库内源字体，启用 Dynamic 与 Multi Atlas，不得回退系统字体。
- 字体与本地化内容只能进入各自专用 Addressables 组；分类标签不得混用。
- 每个本地化可见字符在最终集成前必须能由正式字体解析。
- 三种 Locale 与三档字体缩放必须通过 UI 裁切、溢出和可读性测试。

## 5. LOC-UI-001 设计

- 正式 `UI` 集合包含 191 个简中/英文 Key：152 个既有 UI/存档/平台诊断 Key，加 39 个
  Qinglan HUD、设置、地图、导航与提示 Key。
- 原 M8 混合集合完整保留为 `M8LegacyUI`，继续承载 development-only 内容与编辑器向导测试；
  没有删除其 649 个 Key，也不赋予 Release 标签。
- Pseudo 不维护人工第三张表，而由 `qps-ploc` 的 PreserveTags、Expander、Accenter、
  Encapsulator 从正式英文自动生成。
- 正式 Shared Data、en、zh-Hans 三个运行时资产进入 `QinglanDemo-Localization`，保留 Locale/Preload
  标签，并增加 `pack.qinglan_demo`、`release`、`localization.release`。

## 6. LOC-CONTENT-001 设计

- `QinglanContent` 的清单边界直接取自当前 `QinglanDemoContentPack.baked.json`：193 个内容定义的
  193 个名称 Key、193 个说明 Key，以及 106 条技能 LevelPatch 的玩家可见变更 Key，共 492 Key。
- 名称遵守青岚统一术语表；说明按角色、术式、状态、敌人、地图、局内奖励、局外成长与藏录等
  25 种内容 Kind 编写，禁止原始 Key 回显、`Unavailable` 和任何占位前缀。
- 等级变更 Key 使用稳定格式
  `content.<id>.level.<level>.change.<catalog-patch-index>`，把伤害/效果、冷却、作用范围、目标数和
  生效数量转换为可读的双语升级说明。
- 生成器锁定目录定义数 193、名称/说明数 386、LevelPatch 数 106；目录变化会先使构建与测试失败，
  要求显式补齐术语和文案后再更新基线，防止新内容静默漏译。
- Shared Data、en、zh-Hans 三个正式资产进入 `QinglanDemo-Localization`，地址为
  `QinglanContent_en`、`QinglanContent_zh-Hans` 及稳定 Shared Data 地址，并带
  `pack.qinglan_demo`、`release`、`localization.release` 标签；Pseudo 继续从英文动态生成。

## 7. LOC-NARRATIVE-001 设计

- `QinglanNarrative` 共 123 个双语 Key：3 篇陆青野故事 24 条、6 份旧庭藏录 24 条、三座风脉台
  目标 12 条、三个地图事件 12 条、五处地标 15 条、折枝/听风首领对白 30 条、旧庭开场与收束 6 条。
- 当前 Bake Catalog 引用的 6 个 `story.*` Sequence Key 和 6 个 `collectible.*.body` Key 必须全部命中；
  其余 Key 使用 `narrative.qinglan.<domain>.<id>.<state-or-index>` 约定，为最终演出与状态提示提供稳定入口。
- 剧情身份严格遵守总纲与剧情设定：沈停云是抚养陆青野、后携无名残剑离开的老剑客；听风是由守庭剑傀、
  门人愿念与封庭古誓维系的守誓灵结，不把二者合并为同一角色。
- 目标与事件统一使用 `prompt/progress/complete/failed` 四状态；连续对白使用两位数字顺序号，避免把文案顺序
  硬编码进 UI 或首领专用类。
- Shared Data、en、zh-Hans 三个正式资产进入 `QinglanDemo-Localization`，稳定地址为
  `QinglanNarrative_en`、`QinglanNarrative_zh-Hans` 及 Shared Data 地址，并由 Pseudo Locale 动态生成伪本地化。

## 8. G3.3 最终运行时集成

- `UnityLocalizationService` 按稳定命名空间路由三张正式表：`ui.*` → `UI`，`content.*` →
  `QinglanContent`，`story.*` / `collectible.*` / `narrative.*` → `QinglanNarrative`；en 与 zh-Hans
  六张实际表全部设为 Preload。
- Pseudo 保留 PreserveTags、Expander、Accenter、Encapsulator 四阶段；Accenter 将可见 ASCII 转换为
  Noto CJK 可覆盖的全角字符，以 `【】` 包围，并从正式英文表实时生成，不维护第三份人工正文。
- 运行时页面、HUD、危险提示和池化伤害数字全部使用 TMP；删除 `CreateDynamicFontFromOSFont` 与
  `LegacyRuntime.ttf` 路径。Infrastructure 启动时通过三个稳定 Addressables 地址加载 Sans Regular、
  Sans Bold 与 Serif SemiBold，并作为唯一 Handle Owner 在 Host 销毁时释放。
- 集成器从 191 个 UI Key、492 个内容 Key、123 个叙事 Key 的中英文本、Pseudo 结果和运行时符号收集
  字形，预热 Regular 1093、Bold 563、Serif 842 个字符，共持久化 40 张 Atlas Texture；逐字缺失即
  阻断 Project Validation。
- 非运行页面使用完整横向安全区；HUD 与危险层只在 `RunHud` 激活。Windows Player 在
  1920×1080、150% 字号下实测角色页正文高度 894.2 px、可用高度 904.4 px，无裁切或溢出。
- 长期依赖边界与回滚规则由 `Docs/ADR/0030-g3-3-formal-tmp-localization-runtime.md` 固化；
  `Game.Core` / `Game.Simulation`、Content Schema 6、存档 Schema 3 与 30 Hz Tick 均未改变。

## 9. 最终门禁证据

| 门禁 | 结果 | 证据 |
|---|---|---|
| G3.3 聚焦 EditMode | PASS（4/4） | `TestResults/G33FinalEditMode.xml` |
| G3.3 聚焦 PlayMode | PASS（2/2） | `TestResults/QinglanDemo/G3.3/MarginFixPlayMode.xml` |
| 全量 EditMode | PASS（445/445） | `TestResults/QinglanDemo/G3.3/FullRegressionFinal2/editmode.xml` |
| 全量 PlayMode | PASS（20/20） | `TestResults/QinglanDemo/G3.3/FullRegressionFinal2/playmode.xml` |
| Project Validation | PASS | `TestResults/QinglanDemo/G3.3/validation-post-margin.log` |
| Windows Development Build | PASS | `TestResults/QinglanDemo/G3.3/build-windows-post-margin.log` |
| 构建后 Player Smoke | PASS | `TestResults/QinglanDemo/G3.3/player-smoke-delivery.json` |

完整结果与已发生的失败/修正记录见
`Docs/Reports/2026-08-10-g3-3-formal-font-localization-final-integration.md`。
