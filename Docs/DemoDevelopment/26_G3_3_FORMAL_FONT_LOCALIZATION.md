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
| 4 | LOC-CONTENT-001 | `QinglanContent` 集合不少于 296 个 Key | 待实现 |
| 5 | LOC-NARRATIVE-001 | `QinglanNarrative` 集合不少于 120 个 Key | 待实现 |
| 6 | G3.3 集成 | TMP 运行时、三 Locale、伪本地化、字形与布局门禁 | 待实现 |

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
