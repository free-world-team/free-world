# G4.2-B—F：《剑起青岚》Demo 视觉体验生产设计规范

- 状态：`IMPLEMENTATION / HUMAN SIGNOFF NOT RUN / G4.2-B—D ENGINEERING CLOSED / G4.2-E—F NOT STARTED`
- 制定日期：2026-08-23
- 基线分支：`codex/qinglan-demo-implementation`
- 基线提交：`5369f6a`
- 上位计划：`Docs/DemoDevelopment/31_G4_2_VISUAL_QUALITY_OPTIMIZATION_PLAN.md`
- Canonical Style Bible：`Docs/ArtDirection/G42A/STYLE_BIBLE.md`
- 当前发布判断：`NO-GO`
- 适用目标：Windows x64 / Steam Demo，Unity 6 LTS、URP、uGUI、TMP

## 1. 结论

远端基线已经完成 G4.2-A 的自动实现与验证：Canonical Token、紧凑 HUD、横向三卡、六槽构筑、中央战斗区、
玩家/敌人轮廓、危险填充、拾取密度处理、90 秒固定 Tick Player 门禁和 Release Build 均有实际证据。
独立人类视觉 Rubric 仍为 `NOT RUN`，因此 G4.2-A 仍不宣称 `COMPLETE`。用户随后明确要求全程不审核并持续开发，故 G4.2-B 按用户当前指令实施；该指令只改变执行节奏，不把独立人类签字伪报为通过。

本文不建立新的 G5 里程碑，也不替代 31 号 G4.2 主计划；它把已经确定的“青瓷剑境”方向展开为 G4.2-B—F
可直接实施的 UI、场景、角色、VFX、音频、素材准入和验收规格。若本文与上位计划或 Canonical Style Bible
冲突，以后两者为准。本阶段不改变战斗真值，不以堆叠不一致素材代替艺术指导，也不把开源站点等同于整站授权。

### 1.1 还需要完成的核心工作

| 优先级 | 工作 | 当前判断 | G4.2-B—F 退出条件 |
|---|---|---|---|
| P0 | 统一艺术指导 | G4.2-A Style Bible、Token 和样板已实现；独立人类签字仍 `NOT RUN` | Rubric ≥80/100 且每维 ≥60%，保留独立人类签字 |
| P0 | UI 信息架构和视觉系统 | 样板 HUD、横向三卡和六槽构筑已实现；标题/角色/地图/结算等全流程未重制 | G4.2-B 所有主流程使用同一 Token、图标和组件系统 |
| P0 | 场景空间与 2.5D 统一 | 地面重复、接缝明显，物件比例和透视不统一 | 五区可辨识，地面无明显拼缝，角色接地可信 |
| P0 | 角色、敌人与武器可读性 | 尺寸偏小、剪影和动作差异弱 | 灰度/缩略图下仍能分辨玩家、六敌、两 Boss |
| P0 | VFX 语法 | 发招、命中、危险和奖励层级不足 | 每个主要技能有预兆—释放—命中—残留完整链路 |
| P1 | 动画与镜头反馈 | 动作节奏、受击和 Boss 阶段反馈偏弱 | 动作、震屏、停顿、音效按统一预算协同 |
| P1 | 音频混音和声学标识 | 有音频但缺少优先级与辨识度 | 敌方危险、玩家攻击、UI、奖励在压力场景中可辨 |
| P1 | 可访问性视觉验证 | 功能存在但部分提示常驻、信息过密 | 150% 字号、高对比、减弱闪烁/震动全流程通过 |
| P1 | 正式表现性能复测 | G4.0 的加速截图不能代表目标硬件 | 真实时长目标机 GPU、1% Low、GC 和过绘预算通过 |
| P2 | 氛围、据点和转场润色 | 只达到功能表达 | 环境动态、转场和停留页面达到统一展示质量 |

## 2. 范围与非范围

### 2.1 范围

- 以 G4.2-A Canonical Style Bible 为真值，补全 UI Design System、VFX Style Guide 和 Audio Mix Guide；
- 重做标题页、角色/地图/配装、战斗 HUD、升级/奖励、暂停、结算和设置页面的布局与视觉；
- 在现有倾斜正交架构内统一地面、场景物件、角色、敌人、Boss、武器、阴影、光照和排序；
- 为主要技能、敌方危险、死亡、奖励和 Boss 阶段建立完整的反馈链；
- 评估可商用开源/公共领域资源，建立逐项准入清单，但不在本规划任务中导入；
- 完成多分辨率、无障碍、性能、Player 实机和人工视觉审查门禁。

### 2.2 非范围

- 不改变 30 Hz Simulation Tick、战斗公式、稳定 ContentId、存档格式或胜负规则；
- 不把项目整体改成全 3D，也不迁移到 UI Toolkit、HDRP、Entities 或新渲染后端；
- 不引入第三方运行时代码、Unity Package、Prefab、Shader 或插件；
- 不复制或改造参考商业游戏及开源游戏的角色、场景、音效、字体、动画、Logo、代码或品牌；
- 不把参考游戏截图、开源项目画面或来源不明图片作为 AI 生成输入；
- 不在一个工作包中批量导入未经逐项审查的素材包；
- 不把自动截图计数、素材数量或 Codex 审阅替代独立人类最终签字。

## 3. 现状证据与问题审计

本审计基于以下当前实机证据：

- `TestResults/QinglanDemo/ManualLaunch/window.png`
- `TestResults/QinglanDemo/G4.0/Screenshots/00-enter-combat.png`
- `TestResults/QinglanDemo/G4.0/Screenshots/15-seconds.png`
- `TestResults/QinglanDemo/G4.0/Screenshots/30-seconds.png`
- `TestResults/QinglanDemo/G4.0/Screenshots/45-seconds.png`
- `TestResults/QinglanDemo/G4.0/Screenshots/60-seconds.png`

上述截图是原始问题基线；G4.2-A 已完成样板级 HUD、三卡、中央战斗区、轮廓、危险与密度优化。提交事实和
机器 Hash 记录见 `Docs/Reports/2026-08-13-g4-2-a-visual-target-and-sample.md`。该报告引用的
`TestResults/QinglanDemo/G4.2-A-Release-Final` 属于 Git 忽略的机器证据，当前工作区没有随 Fetch 获得；
需要逐图人工签字时必须从原执行机转移证据或按报告命令重跑，不得把报告文字冒充原始截图。
下表仍作为 G4.2-B—F 的全量生产缺口使用，不得把样板优化外推为全页面、五区、六敌和两 Boss 已完成。

当前 `Assets/GameAssets/Final` 已有大量正式 PNG 和音频。由此可以确认：问题不是“完全没有美术”，而是
画面构成、资产风格、UI 组件和反馈节奏缺少统一标准。

| 模块 | 实机症状 | 根因判断 | 优化方向 |
|---|---|---|---|
| 标题页 | 高细节背景上叠加大面积半透明灰板，标题和按钮像工具菜单 | 缺少主次关系、品牌锁定和背景保护区 | 重做品牌锁定、主行动区、渐变遮罩和焦点状态 |
| 战斗 HUD | 左上长条与调试数字、右上多行目标、底部长技能条同时抢占画面 | 所有信息都被当作常驻一级信息 | 压缩为生命/经验、时间/波次、上下文目标三层 |
| 教程/无障碍提示 | 大块警告长期占据右下角 | 教程与状态提示没有生命周期 | 首次短 Toast，5 秒后收起，可在帮助页重看 |
| 技能与图标 | 多个栏位重复通用图形，辨识依赖文字 | 缺少语义图标体系和品质/类型编码 | 一技能一剪影；形状、颜色、边框三重编码 |
| 地面 | 大面积规则重复、轨道/接缝明显，像平面拼图 | 宏观变化、边缘过渡和 Decal 不足 | 3—5 变体、宏观遮罩、顶点色、Decal、边缘过渡 |
| 场景物件 | 比例、视角、材质和光向不一致 | 不同资源未经过统一重绘/材质校准 | 统一相机视角、像素密度、光向、轮廓和接触阴影 |
| 玩家与敌人 | 实机尺寸偏小，成群时剪影相似 | 阅读距离、轮廓矩阵和色彩职责未锁定 | 提升屏幕尺寸，建立玩家/普通/精英/Boss 剪影层级 |
| 武器 | 武器虽存在，但持续可见性和动作关系弱 | 仅完成存在性，没有完整动作姿态和发光层 | 武器挂点、收招/挥击、拖尾、命中与音效共同设计 |
| VFX | 细环、重复叶片和弱命中覆盖过多技能 | 缺少时序语法和技能独有 Profile | 预兆、释放、运动、命中、残留五阶段；按危险分层 |
| 光照/后处理 | 角色和地面像分离图层，深度感不足 | 法线、接地、主光、雾和色彩分级未形成系统 | Sprite Lit/自定义材质、接触阴影、轻量 Bloom/Color Adjust |
| 音频 | 已有大量 Clip，但高压战斗中语义层级仍待人工验证 | 资产数量不等于混音清晰 | 设置优先级、并发上限、Duck、危险签名和响度预算 |
| 交互 | 功能可点，但布局更像逐项菜单 | 缺少空间导航、状态预览和操作反馈 | 卡片化比较、显式焦点、即时预览、返回路径一致 |

## 4. 目标艺术方向

### 4.1 核心命题

**青瓷剑境：旧庭复苏**：青岚穿过残庭，风、叶、剑痕和灵光共同构成战斗阅读语言。画面应明净、
有层次、有古意，但不能落入灰暗写实、通用西幻、纯水墨黑白或科技霓虹。

### 4.2 四个视觉支柱

1. **墨线剪影**：角色与物件先靠外轮廓被识别，再用内部细节丰富；缩小后仍可读。
2. **浅浮雕空间**：地面是真正承载空间的 3D/XZ 平面，竖直 Sprite 和低模物件具有统一接触阴影和光向。
3. **风的节奏**：布、叶、雾、技能拖尾和 UI 动效遵循同一左下至右上的流向，不随机抖动。
4. **信息克制**：战斗中心让给角色和威胁；UI 只在需要时出现，颜色只承担稳定语义。

### 4.3 禁止方向

- 不混用写实 PBR 地面、像素角色、扁平矢量 UI 和厚涂背景而不做统一处理；
- 不以满屏高饱和 Bloom、持续震屏、过长拖尾掩盖动作和敌方危险；
- 不让装饰色与危险色相同；
- 不在正式界面显示内部枚举、裸数值调试串、文件名或 Localization Key；
- 不在五个区域仅通过换色区分；每区必须同时有地形、轮廓、装饰和氛围差异。

## 5. Design Token 与视觉规范

### 5.1 基础色板

| Canonical Token | 值 | 唯一职责 |
|---|---|---|
| `Ink-950` | `#102A2D` | 最深背景、轮廓、遮罩 |
| `Ink-800` | `#1C4243` | 面板、次级边缘、暗部 |
| `Jade-500` | `#42B8AD` | 交互、进度、玩家识别 |
| `Jade-200` | `#A9E5D8` | 高光、焦点、辅助边缘 |
| `Rice-100` | `#F2EBD8` | 正文、关键图标、明部 |
| `Gold-400` | `#D7B55A` | 稀有、完成、阶段节点 |
| `Cinnabar-500` | `#E4573D` | 危险主体、重击 |
| `Cinnabar-300` | `#FF8B62` | 危险前沿、警告脉冲 |
| `Void-700` | `#563C76` | Boss/异常机制，不与普通危险混用 |

颜色不得单独承担状态。危险、品质、选中、禁用必须至少再使用形状、图标、描边或动效之一。正文和关键
信息以 4.5:1 对比度为最低目标；大字、装饰图形可以按适用标准另测，但不能用高对比模式弥补默认界面。

### 5.2 字体与字号

- 品牌/章节标题：Noto Serif CJK SC，SemiBold/Bold；
- 正文/数据/按钮：Noto Sans CJK SC，Regular/Medium；
- 1920×1080 基准字号：品牌 48—64、页面标题 32、卡片标题 22、正文 18、辅助 16；
- 关键交互正文不低于 16；150% 模式必须重新布局，不允许单纯放大后裁切；
- 数字使用等宽数字特性或稳定宽度容器，避免生命/计时跳动造成布局抖动；
- 中英字体、字重、行高和回退链保持现有 provenance 与许可证门禁。

### 5.3 间距、形状和组件

- 使用 8 px 网格，4 px 只用于图标内部微调；
- 页面安全边距：1080p 至少 48 px，720p 至少 32 px；
- 常规按钮高度 52—60 px，手柄/触控等价命中区不小于 44×44 px；
- 面板优先使用可拉伸九宫格、细内描边和低透明纸纹，不使用大面积无层次灰色矩形；
- 焦点同时具有 2 px 高对比描边、轻微放大和方向提示，不能只靠颜色；
- 禁用态降低饱和而非降低到不可读；按钮文字仍需满足对比度。

### 5.4 动效 Token

| Token | 时长 | 用途 |
|---|---:|---|
| `Motion/Instant` | 90 ms | Hover、轻触反馈 |
| `Motion/Fast` | 160 ms | 焦点移动、Toast 入场 |
| `Motion/Base` | 240 ms | 卡片展开、页面元素过渡 |
| `Motion/Slow` | 360 ms | 页面切换、奖励揭示 |

默认使用 Ease Out 进入、Ease In 离开；禁止无意义循环缩放。开启“减弱动态效果”后，位移动画缩短到 0—90 ms，
震屏为 0，闪烁以稳定描边/明度变化替代。

## 6. UI/UX 重制设计

### 6.1 技术边界

- 保留现有单一 Screen Presenter、Command 和状态来源；不建立第二套 UI 真值；
- 保留 uGUI + TMP 和单主 Canvas，不迁移 UI 后端；
- G4.2-A 已实现 `Game.UI.QinglanUiTheme` 与 `Game.Presentation.QinglanPresentationTheme`；B—F 只扩展既有 Token，并保持两侧镜像测试；
- 仅在既有结构不能表达时新增 `QinglanUiSpriteCatalog` 或 `QinglanUiMotionProfile`，不得复制第三套 Theme；
- UI 资产只引用 Presentation 层内容，不进入 `Game.Core` 或 Simulation；
- 如果现有表现 Schema 不能表达主题和语义图标，先提交 Change Request，不在具体页面硬编码绕过。

### 6.2 标题页

- 以角色/庭院视觉焦点作为主画面，使用局部暗角或渐变保护文字，不覆盖巨大不透明面板；
- Logo 锁定在上中或左上品牌安全区；主行动最多三个：继续/开始、设置、退出；
- 首次启动展示“开始游戏”，有有效 Profile 时展示“继续准备”；
- 版本、语言、无障碍入口放在次级区域，不与主行动争夺层级；
- Hover/Focus 展示轻微剑气扫光和短促 UI 音，不使用强烈发光循环；
- 背景动效只包含低频雾、叶和光斑，减弱动态效果时自动静止。

### 6.3 角色、地图与配装

- 角色页采用“立绘/剪影 + 身份 + 核心机制 + 起始技能”结构，而不是属性表堆叠；
- 地图页突出区域轮廓、局长、Boss、危险和奖励，不在卡片上放完整规则说明；
- 配装页将主动技能、被动/心诀、协同预览分区；锁定原因使用本地化短句；
- 选中卡片有边框、浮起、标题和摘要四重反馈；手柄焦点不会跳到不可见控件；
- 角色、地图、配装之间保留清晰的步骤标记和可逆返回，不丢失已选内容。

### 6.4 战斗 HUD

| 区域 | 默认常驻 | 条件显示 |
|---|---|---|
| 左上 | 生命/护盾、经验、等级 | 受击 1.5 秒显示具体变化 |
| 上中 | 时间、波次/阶段 | Boss 出现时切换为 Boss 条 |
| 右上 | 单行当前目标摘要 | 展开键查看完整目标列表 |
| 下方 | 主要技能 4—6 槽、冷却/等级 | 被动和协同在详情页展开 |
| 世界空间 | 拾取、伤害、危险预兆 | 按密度和优先级聚合/丢弃 |

- 默认不显示内部帧率、模拟数值、实体统计或长调试串；Debug HUD 只在开发构建显式开启；
- 教程提示最多展示 5 秒，完成输入后立即消失；可在暂停页“操作说明”重看；
- Boss 条只在有效 Boss 生命周期存在；阶段变化使用短标题、条纹和声音，不遮挡角色；
- 升级可用、奖励掉落、低生命和敌方危险使用不同位置与声学签名，避免全部在屏幕中心弹出；
- 下方技能栏不再横跨几乎全屏；无输入时降低背景不透明度，冷却信息保持清晰。

### 6.5 升级、奖励与结算

- 每张卡片包括：独有图标、名称、类别、等级/品质、两行效果摘要、变化值、标签；
- 变化值使用 `旧值 → 新值` 或 `+百分比`，不能让玩家自行对照长段文字；
- 三选一卡片保持同宽、同信息槽位；稀有度只影响边框和轻量粒子，不改变可读性；
- Evolution/Synergy 使用独立的合成关系图，不复用普通升级颜色；
- 结算先给胜负和关键构筑，再给详细统计；“再来一局”和“返回据点”为清晰主次行动；
- 所有页面支持鼠标、键盘、手柄，并由同一 Presenter 产生相同命令。

### 6.6 设置与无障碍

必须提供并实际验证：

- UI 缩放 100% / 125% / 150%；
- 高对比主题；
- 减弱屏幕震动、闪烁和动态效果；
- 伤害数字：全部/关键/关闭；
- 敌方危险轮廓强度；
- 色觉友好危险纹理；
- 音乐、SFX、UI、语音/提示（如有）独立音量；
- 手柄重映射或清晰的 Input System 映射入口；
- 字幕/文本提示背景与持续时间。

### 6.7 分辨率矩阵

至少捕获并人工审查以下界面的截图矩阵：1280×720、1920×1080、2560×1440、3440×1440、1280×800；
每个分辨率覆盖标题、角色、配装、战斗、升级、奖励、暂停、设置和结算。超宽屏扩展背景，不把关键 UI 推离
16:9 安全区；Steam Deck 类 1280×800 只作为布局证据，不等于性能认证。

## 7. 场景、光照与 2.5D 深度

### 7.1 地面系统

- 每个区域至少 3—5 个可无缝基础变体，避免单张大图规则重复；
- 使用宏观色调遮罩、顶点色或等价低成本方式打破重复，不增加 Simulation 数据；
- 道路、庭院、腐化、边缘和受击痕迹使用 Decal 或表现层投影；
- 不允许明显格线、轨道、黑缝、纹理方向突变和相邻贴图曝光差；
- 地面细节密度在角色脚下低于外围，确保危险圈和掉落物可读。

### 7.2 场景物件

- 所有物件按统一相机角度、地面接触点、像素密度、主光方向和轮廓粗细校准；
- 建立小/中/大三级轮廓：碎石和草丛、栏杆/树/碑、地标/建筑；
- 同类物件保留 2—4 个轮廓变体，禁止纯旋转复制制造“风车式”重复；
- 遮挡玩家的前景物件按透明规则淡出，仍保留接地和空间提示；
- 装饰物不得拥有碰撞或寻路真值，除非内容定义已经明确；表现不能改变 Walkable 结果。

### 7.3 五区差异

每区必须至少在以下四项中有三项独有：地面结构、竖直轮廓、环境粒子、光色、地标、边缘过渡。推荐：

| 区域职责 | 地面/结构 | 氛围 | 识别地标 |
|---|---|---|---|
| 起始庭院 | 温石与浅苔、秩序最完整 | 柔和青风、少量落叶 | 残门/剑台 |
| 竹影道 | 狭长纹理与斜向阴影 | 竹叶、风带 | 竹门/倒竹 |
| 残碑区 | 破碎石板、裂隙 | 低雾、纸符 | 高碑/断阶 |
| 灵池区 | 冷色湿地、反光边缘 | 微光孢子、水雾 | 池心石/小桥 |
| Boss 庭 | 开阔圆形构图、危险边界 | 阶段风暴 | 中央祭台/巨树 |

名称可与现有内容定义调整，但视觉职责不可全部退化为换色。

### 7.4 光照、材质和后处理

- 使用一个稳定主光方向，角色、场景、UI 插画均遵守；
- Sprite Lit Shader Graph 可使用法线与 Mask 提供浅浮雕受光，Alpha Clipping 需避免细线消失；
- 角色和重要敌人使用稳定接触阴影/Blob Shadow，地面投影与脚点一致；
- Decal Renderer Feature 只用于地面污渍、道路过渡、危险残留等受控用途；
- Global Volume 仅启用轻量 Bloom、Color Adjustments、Vignette；禁止用强后处理掩盖资产不统一；
- Bloom 只服务灵光、剑锋、奖励和高优先级命中，普通地面与 UI 正文不进入高亮；
- 自定义光照如需新增 Shader/Renderer Feature，必须先评估性能、兼容和 ADR 需求。

## 8. 角色、敌人、Boss 与武器

### 8.1 屏幕比例与接地

以 1920×1080 战斗截图为基准：

- 玩家可见主体高度目标 96—120 px；
- 普通敌人 72—104 px，精英 104—136 px；
- Boss 180—280 px，阶段变化不通过无限放大表达；
- 影子宽度约为脚点主体宽度 55%—80%，按悬浮/飞行类型调整；
- 所有方向帧使用一致 Pivot、Feet Marker、Weapon Socket 和 Bounds；
- 镜头缩放变化时优先保证玩家、危险预兆和 Boss 攻击可读。

这些数值是 Golden Scene 的初始验收目标，不是 Simulation 尺寸。

### 8.2 剪影矩阵

- 玩家：直立、长剑斜向、青色风带；
- 近战小怪：前倾、低重心、短攻击范围；
- 远程小怪：高/窄轮廓，武器或蓄力点清晰；
- 冲锋类：横向宽、头部/肩部形成方向箭头；
- 支援类：顶部或背部有稳定识别物；
- 精英：在原轮廓上增加单一醒目标志，不能只换颜色；
- 折枝、听风：轮廓、主色、阶段纹理、攻击预兆互不复用。

需要执行 25% 缩略图、灰度、去内部颜色和 1 秒闪现识别测试。若六敌中任意两类主要靠名称才能区分，
则剪影门禁 `FAIL`。

### 8.3 动画集合

玩家、六敌、两 Boss 至少具有：Idle、Move、Attack Wind-up、Attack/Release、Hit、Death；Boss 另有 Phase。
动画继续由集中帧驱动器更新，不为每个敌人增加 `Animator` 或 `MonoBehaviour.Update`。表现层插值、受击停顿、
闪白和抖动不得改变 Simulation Tick 或伤害结算。

### 8.4 武器表现

- 游风剑在 Idle、Move、释放和收招时均有明确挂点；
- 近战轨迹由动作弧线决定，不以巨大圆环替代剑身运动；
- 投射技能具有独有头部、尾迹和旋转规则；
- 武器亮度只在释放窗口提升，常驻亮度不得与掉落/危险争夺；
- 命中反馈由武器停顿、火花/风裂、受击轮廓和声音共同完成；
- Weapon View 仍是 Presentation，不写回 Simulation Transform 或冷却真值。

## 9. VFX 与战斗反馈规范

### 9.1 五阶段语法

| 阶段 | 建议时长 | 必须回答的问题 |
|---|---:|---|
| Anticipation | 100—350 ms | 谁要做什么、危险在哪里 |
| Launch | 60—120 ms | 动作从哪里释放、方向是什么 |
| Travel/Sustain | 依技能 | 轨迹、范围和所有权是否清楚 |
| Impact | 80—220 ms | 是否命中、强度和结果是什么 |
| Residue | 150—500 ms | 余波是否仍危险、何时结束 |

玩家快速基础攻击可以缩短 Anticipation，但敌方高伤技能不得省略。地面预兆和实际伤害范围必须使用同一
Presentation 投影边界，不能为了好看扩大到误导玩家。

### 9.2 语义色与层级

- 玩家普通攻击：青绿 + 米白；高阶/进化：青绿 + 金；
- 敌方普通伤害：朱红；高危险/Boss：朱红 + 深墨 + 条纹；
- 治疗/护盾：低饱和青蓝，形状与攻击不同；
- 奖励/稀有掉落：金色向内汇聚，不使用敌方爆炸形状；
- 中性世界交互：雾蓝；
- 白色只作瞬时高光，不作为持续大面积填充。

排序建议：地面装饰 → 地面危险/范围 → 掉落 → 角色阴影 → 角色/敌人 → 武器/投射物 → 命中高光 →
前景遮挡 → 世界空间 UI → 屏幕 UI。P0 危险预兆永不因池满被静默丢弃。

### 9.3 技能独有性

每个玩家可见主要技能必须有独有的：

- 图标剪影；
- 释放形状；
- 运动轨迹或铺场边界；
- 命中形状；
- 主音色；
- 升级后可见变化。

不得继续使用同一叶片或同一圆环仅换颜色覆盖多个技能。可以共用底层 Shader、粒子模块和图集，但最终
Profile 必须在轮廓、节奏或音色上至少有两项差异。

### 9.4 镜头、停顿和震屏预算

| 强度 | 振幅参考 | 时长 | 示例 |
|---|---:|---:|---|
| Micro | 0.02—0.04 | 60 ms | 普通近战命中 |
| Medium | 0.05—0.08 | 100 ms | 精英死亡、强技能 |
| Boss | 0.10—0.14 | 180 ms | 阶段冲击、最终击杀 |

- 同一 250 ms 窗口只保留最高优先级震屏，不叠加振幅；
- Presentation-only Hit Stop 建议 25—60 ms，不暂停 Simulation，只冻结或减速视觉采样；
- 减弱震动模式振幅为 0；减弱闪烁模式用轮廓和粒子方向替代全屏 Flash；
- 全屏色闪只允许 Boss 阶段/结算等极少事件，且必须通过光敏感性审查。

### 9.5 性能预算

- 继续使用现有 VFX Pool，P0/P1/P2 分级；
- 1080p 高压场景平均透明过绘目标不高于 3×，局部峰值必须短暂且可解释；
- Draw Call 目标不高于 500，具体以当前目标机基线和 SRP Batcher 证据校准；
- 高频材质实例化、字符串属性查找、运行时纹理创建和逐实体粒子组件创建为禁止项；
- 图集按采样和材质职责拆分，避免单张超大图集因一个特效常驻内存；
- 超出预算时先降低次级粒子、残留时长和远距离装饰，不削弱危险预兆。

## 10. 音频体验重制

### 10.1 混音层级

优先级从高到低：敌方致命预兆 → 玩家受击/低生命 → Boss 阶段 → 玩家主要命中 → 奖励/升级 → 普通敌人死亡 →
环境与装饰。并发到上限时按此顺序保留，不以 Clip 创建先后决定。

### 10.2 声学签名

- 玩家剑与风：短金属瞬态 + 空气切割 + 低音量尾流；
- 敌方近战：更干、更靠中低频；
- 敌方远程/蓄力：攻击前必须有可辨识上升或脉冲；
- Boss 折枝/听风：不同音色家族，阶段切换具有唯一签名；
- UI Hover、Confirm、Back、Error、Reward 使用五个稳定语义，不复用战斗爆点；
- 环境风声采用长周期变化，不与技能风声争夺相同频段。

### 10.3 初始响度目标

以下只作为调音起点，必须在实际设备和高压战斗中复核：音乐约 -18 LUFS-I、Master Ceiling -1 dBTP、常规
SFX 峰值约 -6 dBFS。玩家低生命、奖励揭示和暂停可使用短暂 Duck，不允许长时间抽吸。最终以实机可辨性、
无削波和无疲劳为准，不把单一响度数字视为通过。

## 11. 开源与公共领域素材候选

### 11.1 原则

“公开可下载”不等于“允许商业使用”。每一个具体文件都必须按其来源页面、许可证文本、作者、下载日期、
原文件 Hash 和修改记录逐项审核。下表只是候选源评估，不是导入授权，也不代表整站内容均可使用。

| 来源 | 页面声明 | 推荐用途 | 决策 | 主要风险/要求 |
|---|---|---|---|---|
| ambientCG | 站点资产 CC0 | 石、泥、苔、木、纸等基础纹理 | `PREFERRED CANDIDATE` | 下载具体资产并留存版本、许可证和 Hash；重绘为统一风格 |
| Poly Haven | 资产 CC0 | 少量 HDRI/纹理/基础模型参考 | `PREFERRED CANDIDATE` | API 条款独立；不自动批量抓取，逐页下载和登记 |
| Quaternius | FAQ 声明模型 CC0 | 原型/基础低模道具几何 | `CONDITIONAL` | 需要重拓扑/重材质/比例统一；不能让低模西幻风直接进入成品 |
| Kenney | 资产页 CC0 | 输入提示、线框和 UI 原型验证 | `PROTOTYPE ONLY` | Adventure UI 风格与本作不一致；不得使用 Kenney Logo |
| Game-icons.net | 多为 CC BY 3.0 | 信息架构与语义图标原型 | `LEGAL REVIEW` | 需要署名、许可链接和改动说明；分发/DRM 兼容需法律复核 |
| Freesound | 文件采用多种 CC 许可证 | 单个环境/SFX 候选 | `ITEM-BY-ITEM ONLY` | 禁止 NC；BY 需署名；逐文件核验，不能按搜索页推断 |
| OpenGameArt | 文件采用多种许可证 | 个别 CC0 候选 | `LOW PRIORITY` | BY/SA/GPL 与信用、源文件、DRM 条款复杂；只优先单项 CC0 |
| itch.io CC0 页面 | 作者自报且页面质量不一 | 仅补充候选 | `HIGH RISK` | 部分页面出现“CC0”与禁止再分发并存；必须以随包许可和快照复核 |
| 项目 FirstParty/AI | 现有管线 | 角色、敌人、Boss、品牌 UI、VFX | `PRIMARY FINAL PATH` | 继续执行 AI/FirstParty provenance、输入权利、人工修改和 Hash 门禁 |

### 11.2 推荐组合

- **最终角色、敌人、Boss、技能图标、品牌 UI、关键 VFX**：继续 FirstParty/合规 AI 生产，保证独有性；
- **环境基础纹理**：从 ambientCG 或 Poly Haven 逐项选择 CC0，重绘/烘焙为本作色彩和受光；
- **环境基础几何**：Quaternius 只作为少量低模底模候选，统一比例、轮廓、UV、材质和碰撞；
- **UI 原型**：Kenney 可用于交互和布局验证，正式版必须替换成项目独有视觉；
- **图标**：优先 FirstParty；Game-icons 仅在法律接受 CC BY 归属与分发条件后使用；
- **音频**：优先现有 FirstParty/AI Catalog；Freesound 仅补充单个明确 CC0 或已审批 CC BY 文件。

### 11.3 明确禁止

- 不下载或导入参考商业游戏的任何文件；
- 不从 GitHub、资源站聚合包、网盘或二次转载页取得来源不明资产；
- 不使用标注 `NC`、`Non-Commercial`、仅个人使用或禁止游戏分发的资源；
- 不因作者口头描述“免费”而忽略随包许可证冲突；
- 不把 CC BY、CC BY-SA、GPL、OGA-BY 等一律当作 CC0；
- 不从候选站点批量爬取全部资产后再筛选；
- 不把 Logo、商标、真实人物、第三方角色或品牌元素当作 CC0 普通素材。

## 12. 资产准入与治理流程

### 12.1 每项外部资产的 Change Request

导入前必须提交 `Templates/CHANGE_REQUEST_TEMPLATE.md`，至少包含：

1. 资产名称、作者、原始 URL、下载日期和具体版本；
2. 原始许可证全文/文件和来源页快照；
3. 是否需要署名、链接、修改声明、源文件或相同许可分发；
4. 商业、Steam、DRM、修改和全球分发是否允许；
5. 原文件 SHA-256、处理后 SHA-256 和修改步骤；
6. 预定目录、Addressables Group/Label 和 `ContentId` 关联；
7. 是否包含代码、Shader、Prefab、脚本、字体、音频或商标；
8. 替代方案和回滚方式；
9. 法律/制作审核人及结论。

### 12.2 目录与登记

- 原始第三方文件：`Assets/ThirdParty/<Provider>/<Pack>/<Version>/...`；
- 原始 License 与 Notice 与文件同层或稳定关联；
- 在 `THIRD_PARTY_NOTICES.md` 登记具体资产，不只登记站点；
- 在 `ASSET_PROVENANCE.csv` 登记来源、作者、许可、源/输出 Hash、修改、用途和审核状态；
- 由第三方源派生的贴图/模型不能伪装为 FirstParty；派生链必须可追溯；
- 未完成审核的文件不得进入 `release` Addressables Group/Label。

### 12.3 第三方代码和渲染扩展

本计划不批准任何第三方运行时代码、Shader、Unity Package、Prefab 脚本或编辑器扩展。若候选资产包包含这些内容，
默认删除且不导入；确有必要时单独 CR，并按 `AGENTS.md` 判断是否需要 ADR。美术包的导入不得隐式改变 Assembly、
Renderer Feature、URP Asset、Input、Localization 或 Addressables 架构。

## 13. 表现层技术设计

### 13.1 建议资产

| 资产 | 职责 | 禁止承载 |
|---|---|---|
| `QinglanUiTheme` / `QinglanPresentationTheme` | G4.2-A 已实现的 Canonical Token 镜像；B—F 继续扩展并由测试防漂移 | 游戏状态、解锁、数值规则 |
| `QinglanUiSpriteCatalog` | 稳定语义到 UI Sprite 的只读映射 | 运行时索引存档 |
| `QinglanUiMotionProfile` | 页面和组件动效 Token、减弱动态版本 | Simulation 时间 |
| `QinglanWorldArtProfile` | 区域材质、地面变体、Decal、氛围预算 | Walkable、出生或碰撞真值 |
| `QinglanVfxStyleProfile` | 阶段、颜色、排序、LOD、无障碍替代 | 伤害范围或命中判定 |
| `QinglanAudioMixProfile` | 路由、优先级、并发、Duck 参数 | 战斗事件真值 |

资产名称是规划建议。实现前必须确认现有 Profile 能否组合表达；能复用时不新建类型，不能表达时先走 Change Request。

### 13.2 依赖方向

```text
Simulation Snapshot / Event / ContentId
                  ↓
     Application Presenter / Projection
                  ↓
  UI Theme / Visual Profile / Audio Profile
                  ↓
       Pooled View / Canvas / URP / Mixer
```

- `Game.Core` 继续不引用 `UnityEngine`；
- UI/VFX/Audio 不回写战斗真值；
- 所有用户可见文本继续使用 Localization Key；
- 高频敌人继续使用集中批处理/帧驱动，不增加逐敌人 Update；
- 加载继续通过 Addressables/注入 Catalog，不使用 `Resources.Load` 或全局查找；
- 正式降级路径必须可见且可记录，不能悄悄回到程序化 Placeholder。

## 14. G4.2-B—F 实施顺序

每个工作包必须独立提交并 Push；前一包 DOD 未通过不得开始下一包。单次只允许一个活跃里程碑 Owner。
G4.2-A 的实现、自动测试、Release Player 与样板截图已有证据，独立人类 Rubric 仍为 `NOT RUN`。用户明确要求不等待阶段审核并持续到最终实机测试，因此工程执行继续；所有报告仍必须把独立人类签字记为 `NOT RUN`。

### 跨包资产治理门禁

- 每个包只选择该包所需的最小外部资产切片，不按整站或整包导入；
- 导入前完成 CR、License、Notice、Provenance、源/输出 Hash 和负向门禁；
- 环境底材可以评估明确 CC0 候选；角色、六敌、两 Boss、品牌 UI、技能主图和标志性 VFX 以 FirstParty/合规 AI 为主；
- 统一重绘、材质、像素密度、光向和目录后才能进入运行时；
- 每个外部资产必须可以独立移除并回退，未审批文件不得进入 `release` Addressables 输入。

### G4.2-B：UI Design System 与全流程重制

- 在既有 `QinglanUiTheme` 上补齐 Sprite Catalog、Motion Token 和复用组件，不复制 Theme；
- 重做标题、角色、地图、配装、HUD、升级、奖励、暂停、设置、结算和 Hub；
- 替换重复通用图标和常驻调试覆盖，保留 G4.2-A 的六槽构筑和中央净区约束；
- 完成键鼠/手柄焦点、150%/200% 字号、高对比和 Reduce Motion；
- 捕获 720p/1080p/1440p/4K/21:9 与英文/简中/Pseudo 截图矩阵。

**DOD**：所有主流程使用统一组件，无 Localization Key/调试串泄漏，输入、布局和可访问性专项均 `PASS`。

### G4.2-C：环境、光照与五区重制

- 重做地面变体、宏观遮罩、边缘过渡、Decal 和接触阴影；
- 统一场景物件比例、光向、轮廓、遮挡淡出和材质；
- 五区建立可辨识的结构、地标、完成态和氛围；
- 调整 URP Sprite Lit/材质和保守后处理；
- 完成接缝、排序、遮挡、Walkable 对齐和目标机 GPU 初测。

**DOD**：五区盲测可辨，地面无明显接缝，装饰不改变模拟真值，导航可读性和 GPU 初测 `PASS`。

### G4.2-D：角色、敌人、Boss 与武器重制

- 统一玩家、六敌、两 Boss 尺寸、Pivot、影子和剪影；
- 补齐 Wind-up、Attack、Hit、Death、Phase 动画；
- 完成游风剑挂点、动作弧、拖尾和命中链；
- 完成精英词缀和 Boss 阶段的形状化差异；
- 保持集中动画和对象池架构，不增加逐敌人 Animator/Update。

**DOD**：灰度/缩略图识别、方向、动作、受击、死亡、阶段、武器可见性和高压池化均 `PASS`。

### G4.2-E：VFX、反馈与音频混音

- 为全部主要技能建立五阶段 VFX Profile；
- 重做敌方危险、掉落、奖励、死亡和 Boss 阶段反馈；
- 落地震屏、Presentation-only Hit Stop、闪烁和无障碍替代；
- 重混音玩家/敌人/Boss/UI/环境层级；
- 完成并发、丢弃、过绘、Draw Call 和 100 Actor/268 Pickup 以上压力可读性审查。

**DOD**：技能独有性、危险准确性、V0 不丢弃、Reduce Motion、混音压力和性能专项均 `PASS`。

### G4.2-F：集成、性能与展示验收

- 连续真实时长 12 分钟完整局，不使用 3.25× 加速作为最终证据；
- 按 0/15/30/60 秒、3/6/9/12 分钟捕获 Player 截图和指标；
- 执行全量 EditMode、PlayMode、Content Validation、Release Build、独立 Player Smoke；
- 在目标硬件重跑 GPU、1% Low、GC、内存、过绘和 Draw Call；
- 完成独立人类视听、可访问性、法律和商业权利签字。

**DOD**：第 15 节所有门禁均为 `PASS` 才能声明 G4.2 `COMPLETE`；否则保持 `INCOMPLETE`。
## 15. 最终验收门禁

### 15.1 人工视觉与交互

- 标题页不再由巨大灰色面板主导，三秒内能识别品牌和主行动；
- 战斗中心 60% 区域没有常驻菜单式遮挡；
- 教程完成或展示 5 秒后收起；
- 玩家、六敌、两 Boss 在 25% 缩略图和灰度下可分辨；
- 游风剑在移动、释放和收招截图中均可见；
- 每个主要技能的释放和命中不靠文字即可区分；
- 五区盲看截图可分辨至少四区，Boss 庭必须唯一；
- 地面没有连续规则接缝、黑缝、突变曝光和错误透视；
- 升级/奖励卡片可在 3 秒内比较核心变化；
- 键鼠和手柄全流程没有丢焦点、隐藏焦点和死路。

### 15.2 UI 与可访问性

- 五种目标分辨率全部截图通过；
- 英文、简中、Pseudo 和 100%/150% 组合无裁切/重叠；
- 正文/关键交互达到目标对比度；
- 高对比、减弱震动、减弱闪烁、减弱动态、伤害数字模式实际生效；
- 危险不只靠红/绿区分；
- 所有用户可见文本均为 Localization Key 解析结果。

### 15.3 资产与法律

- Release 输入中的每个正式资产都有有效 provenance 和 Hash；
- 第三方文件逐项登记 License、作者、URL、版本和修改；
- `THIRD_PARTY_NOTICES.md` 与实际 Release 输入一致；
- 未审批、NC、来源不明、Hash 不符或许可证冲突会阻断 Release Build；
- AI 资产的工具、模型版本、日期、提示词、输入权利和人工修改完整；
- 独立人类法律/商业权利签字为 `PASS`。

### 15.4 测试、构建和性能

- 相关 EditMode：`PASS`；
- 相关 PlayMode：`PASS`；
- 全量 EditMode/PlayMode：`PASS`；
- Content/Asset/Localization/Addressables Validation：`PASS`；
- Windows x64 Release Build：`PASS`；
- 独立 Release Player Smoke：`PASS`；
- 真实时长 12 分钟 Player：`PASS`；
- 当前表现代码的目标硬件 GPU/1% Low/GC/内存：`PASS`；
- 最低规格物理机器：`PASS`；
- 独立人类视听签字：`PASS`。

任何一项实际未执行只能写 `NOT RUN`，不得根据旧里程碑或自动字段推断为 `PASS`。

## 16. 风险与回退

| 风险 | 早期信号 | 缓解与回退 |
|---|---|---|
| 美术风格继续发散 | 每个页面/区域像不同游戏 | 先冻结 Golden Scene；不通过则停止批量生产 |
| 开源资产许可不清 | 来源页与包内许可冲突 | 默认拒绝；改用 FirstParty/AI 或明确 CC0 候选 |
| UI 重构破坏输入 | 手柄焦点丢失、命令重复 | 保持 Presenter/Command，不复制状态；先做组件专项测试 |
| Sprite Lit/后处理超预算 | 过绘、GPU、Draw Call 激增 | 降低次级层、材质变体和后处理；不削弱危险提示 |
| VFX 好看但误导 | 视觉范围与判定不一致 | 从 Snapshot/事件边界投影；视觉范围自动/人工对齐测试 |
| 动画引入逐实体开销 | Animator/Update 数量增长 | 保持集中帧驱动与池；架构测试阻断回归 |
| 图标仍靠文字 | 缩小后多个技能相同 | 一技能一剪影，执行无文字识别测试 |
| 规划被误认为已完成 | 只有文档或引用报告，没有当前机器原始证据 | 顶部状态与逐包报告同步；自动 PASS、硬件 NOT RUN 和独立人类 NOT RUN 分开记录 |

每个工作包必须可单独回退。第三方资产切片、主题系统、场景材质和 VFX Profile 不得与 Simulation 修改混在
同一提交。任何 Architecture/Schema/Renderer 后端变化按 `AGENTS.md` 新增 ADR。

## 17. 参考原则与权威来源

以下页面只用于验证许可或提取设计原则。商业游戏只参考信息层级、战斗可读性、卡片比较、剪影和节奏，
不下载资产、不临摹具体画面、不复制 UI 布局，不作为 AI 输入。

### 17.1 素材与许可证

- Kenney Support（资产页 CC0、商用/署名说明）：<https://kenney.nl/support>
- Kenney UI Pack: Adventure（CC0、130 files，原型候选）：<https://www.kenney.nl/assets/ui-pack-adventure>
- ambientCG（站点 CC0 资产说明）：<https://ambientcg.com/>
- Poly Haven FAQ（资产 CC0，API 条款另行适用）：<https://docs.polyhaven.com/en/faq>
- Quaternius FAQ（模型 CC0）：<https://quaternius.com/faq.html>
- Quaternius Fantasy Props MegaKit（CC0 候选包）：<https://quaternius.itch.io/fantasy-props-megakit>
- Game-icons.net 与 FAQ（CC BY 3.0）：<https://game-icons.net/>、<https://game-icons.net/faq.html>
- Freesound FAQ（逐文件 Creative Commons 许可证）：<https://freesound.org/help/faq/>
- OpenGameArt FAQ（混合许可证与分发注意事项）：<https://opengameart.org/content/faq>
- Creative Commons CC0 1.0：<https://creativecommons.org/publicdomain/zero/1.0/>
- Creative Commons Attribution 3.0：<https://creativecommons.org/licenses/by/3.0/>

### 17.2 Unity 6 与可访问性

- Unity 6 URP Sprite Lit Shader Graph：<https://docs.unity3d.com/ja/6000.0/Manual/urp/prebuilt-shader-graphs-urp-sprite-lit.html>
- Unity 6 URP Decal Renderer Feature：<https://docs.unity3d.com/kr/current/Manual/urp/renderer-feature-decal-create.html>
- Unity 6 URP Post-processing：<https://docs.unity3d.com/kr/6000.0/Manual/urp/add-post-processing.html>
- Unity 6 URP Custom Lighting：<https://docs.unity3d.com/jp/current/Manual/urp/lighting/custom-lighting-introduction.html>
- Xbox Accessibility Guidelines：<https://learn.microsoft.com/en-us/xbox/accessibility/guidelines>
- Xbox XAG 102 Text Contrast：<https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102>
- Windows Accessible Text Requirements：<https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessible-text-requirements>

### 17.3 同类产品原则参考

- Vampire Survivors：<https://store.steampowered.com/app/1794680/Vampire_Survivors/>
- Halls of Torment：<https://store.steampowered.com/app/2218750/Halls_of_Torment/>
- Death Must Die：<https://store.steampowered.com/app/2334730/Death_Must_Die/>
- Deep Rock Galactic: Survivor：<https://store.steampowered.com/app/2321470/Deep_Rock_Galactic_Survivor/>
- Hades II 官方开发博客：<https://www.supergiantgames.com/blog/hades2-unseen-update/>

页面访问和计划核对日期：2026-08-23。实施时必须再次核验具体资产页面与随包许可证，因为页面、版本和许可
可能发生变化。

## 18. 本规划任务完成定义

本文档定义 G4.2-B—F 的生产明细、技术边界、视觉规范、素材准入和验收门禁。G4.2-B—D 已完成 UI、五区、角色/六敌/两 Boss/游风剑工程实现、全量测试、Release Build 和 90 秒 Player；两 Boss 24 张阶段帧与游风剑攻击 Trail 已进入硬门禁。实机 4K、真实 12 分钟 Boss 流程和独立人类签字仍为 `NOT RUN`。后续按用户持续执行指令从 G4.2-E 开始，严格逐包提交、测试和 Push。
