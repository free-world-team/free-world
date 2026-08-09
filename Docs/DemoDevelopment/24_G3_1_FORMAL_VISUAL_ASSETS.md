# 24 G3.1 正式视觉资产、Provenance 与 Addressables

- 状态：`IN PROGRESS — 20 / 27 ART BATCHES`
- 日期：2026-08-10
- 输入：G2.8 垂直切片、G0.4 Manifest、M13、M15、ADR 0004/0011/0012/0026
- 非范围：G3.2 音频、G3.3 字体/正文、G3.4 平衡、G3.5 目标硬件性能、G3.6 Release

## 1. 目标

按 `Assets/G0_4_ASSET_MANIFEST.csv` 的 27 个 ART 行依次完成正式视觉生产。CSV 每一行是独立可审查
批次；每批只在文件、provenance、导入设置、Addressables 和视觉 QA 全部通过后提交并 Push 一次。

## 2. 前置治理门禁

- 正式记录统一使用 ADR 0026 的 Provenance Schema 2。
- Project Validation 同时扫描 AI、FirstParty 和实际 Release Addressables 输入。
- AI 目录固定为 `Assets/GameAssets/AI/QinglanDemo/<asset-id>/`；第一方固定为
  `Assets/GameAssets/FirstParty/QinglanDemo/<asset-id>/`。
- `source/`、`working/`、Prompt 与 provenance 不加入 Addressables；仅 `final/` 和正式 Profile 入组。
- 视觉文件固定进入 `QinglanDemo-Visual`，地址使用 `qinglan/<category>/<short-name>/<variant>`，
  标签固定为 `pack.qinglan_demo`、`release`、`visual.release`。
- 图片内不得含 UI 正文、Logo 字样、水印或第三方品牌；AI 只消费本仓库文字设计，不上传参考素材。

## 3. 批次顺序

严格按 Manifest 行号执行：ART-CHAR-001 → ART-CHAR-002 → ART-CHAR-003 → ART-ENEMY-001 →
ART-AFFIX-001 → ART-BOSS-001 → ART-BOSS-002 → ART-SKILL-001 → ART-SKILL-002 → ART-STATUS-001 →
ART-PICKUP-001 → ART-RELIC-001 → ART-MAP-001 → ART-MAP-002 → ART-OBJECTIVE-001 → ART-EVENT-001 →
ART-LANDMARK-001 → ART-HUB-001 → ART-META-001 → ART-META-002 → ART-COLLECT-001 → ART-STORY-001 →
ART-UI-001 → ART-UI-002 → ART-UI-003 → ART-UI-004 → ART-UI-005。

第一方 vector/procedural 批次由仓库内确定性脚本生成；AI-assisted 批次使用 ImageGen 且每个独立资产
单独调用。任何混合批次仍以一个 ART 行为提交边界。

## 4. 单批次完成定义

1. 数量、格式、尺寸和 Runtime 预算达到 Manifest 最低值。
2. Prompt/生成规格、源文件、工作文件（如有）、最终文件与 Schema 2 provenance 同批保存。
3. 无参考输入时 `referenceInputs=[]`；工具未提供 Seed 时记录明确的不可用原因，不伪造 Seed。
4. 源与最终 SHA-256 实际匹配；生成日条款 URL/日期和商业使用复核完整。
5. Alpha、边缘、锚点、帧间连续性、轮廓、灰阶、色觉/高对比和图片内文字检查通过。
6. Runtime Texture 通常不超过 2048，动画/UI Atlas 关闭 MipMap 并保留 4 px 边缘。
7. 只有 final/Profile 是明确文件级 Addressables，Group、地址和三个标签正确。
8. Focused EditMode、Project Validation 和适用 PlayMode 通过；随后该 ART 行单独提交、单独 Push。

## 5. 最终 G3.1 门禁

27 个 ART 批次全部通过后，运行全量 EditMode/PlayMode、Project Validation、Addressables/Pack 验证和
Windows x64 Development Build，并对正式内容下的 1080p 标准/高对比截图执行人工视觉复核。
GPU/1% Low、正式音频、字体、Release Manifest 和平台合规不得在 G3.1 宣称通过。

## 6. 批次台账

| 顺序 | 批次 | 状态 | 交付与证据 |
|---:|---|---|---|
| 0 | G3.1 Governance | PASS | ADR 0026；Schema 2；AI/FirstParty/实际 Release 输入门禁；提交 `eee9895` |
| 1 | ART-CHAR-001 | PASS | 陆青野 1536×1024 RGBA Atlas；24 个 256×256 Sprite；正式 VisualProfile；EditMode 298/298、PlayMode 17/17、Validation PASS |
| 2 | ART-CHAR-002 | PASS | 陆青野 1024×1024 透明肖像与 256×256 双色轮廓；EditMode 300/300、PlayMode 17/17、Validation PASS |
| 3 | ART-CHAR-003 | PASS | 4 张 1024 FirstParty 源图与 4 张 512 正式乘风档叠加图；EditMode 303/303、PlayMode 17/17、Validation PASS |
| 4 | ART-ENEMY-001 | PASS | 6 张 1024×1024 敌人 Atlas、96 个语义 Sprite、6 个正式 VisualProfile；EditMode 307/307、PlayMode 17/17、Validation PASS |
| 5 | ART-AFFIX-001 | PASS | 狂奔/结界/分裂/震地各 1 张 1024 FirstParty 源图与 512 final；EditMode 310/310、PlayMode 17/17、Validation PASS |
| 6 | ART-BOSS-001 | PASS | 折枝/听风各 1 张 4096×2048 master、2048×1024 final、32 语义 Sprite 与正式 Profile；EditMode 314/314、PlayMode 17/17、Validation PASS |
| 7 | ART-BOSS-002 | PASS | 折枝/听风各 3 张阶段与 Telegraph Overlay；6 张 2048 源图、6 张 1024 final；EditMode 317/317、PlayMode 17/17、Validation PASS |
| 8 | ART-SKILL-001 | PASS | 六基础武器各 1 张 1024 source、512 final 与正式 Profile；EditMode 321/321、PlayMode 17/17、Validation PASS |
| 9 | ART-SKILL-002 | PASS | 六显化各 1 张 2048 source、1024 final 与正式 Profile；EditMode 325/325、PlayMode 17/17、Validation PASS |
| 10 | ART-STATUS-001 | PASS | 七状态＋两伤害策略各 1 张 1024 source、512 final；EditMode 328/328、PlayMode 17/17、Validation PASS |
| 11 | ART-PICKUP-001 | PASS | 六即时灵物各 1 张 256 Sprite＋128 Icon；EditMode 331/331、PlayMode 17/17、Validation PASS |
| 12 | ART-RELIC-001 | PASS | 六战斗奇物各 1 张 256 Icon；EditMode 334/334、PlayMode 17/17、Validation PASS |
| 13 | ART-MAP-001 | PASS | 五区域各 1 张 4096×1024 working master、2048×512 final、16 个 256×256 Tile；EditMode 337/337、PlayMode 17/17、Validation PASS |
| 14 | ART-MAP-002 | PASS | 五区域各 1 张 4096×1024 working master、2048×512 final、16 个 256×256 Prop；EditMode 340/340、PlayMode 17/17、Validation PASS |
| 15 | ART-OBJECTIVE-001 | PASS | 听风/引风/止衡三座风脉台各 1 张 3×1、960×320 三态 Atlas；EditMode 343/343、PlayMode 17/17、Validation PASS |
| 16 | ART-EVENT-001 | PASS | 风脉暴动/药圃复苏/旧剑共鸣各 1 张 4×1、1024×256 四相 Atlas＋Area Profile；EditMode 347/347、PlayMode 17/17、Validation PASS |
| 17 | ART-LANDMARK-001 | PASS | 五种地标各 1 张 3×1、960×320 Undiscovered/Discovered/Claimed Atlas；EditMode 350/350、PlayMode 17/17、Validation PASS |
| 18 | ART-HUB-001 | PASS | 问脉台/藏卷楼/百器阁/万象阁各 1 张 1024 Panel＋256 Icon；EditMode 353/353、PlayMode 17/17、Validation PASS |
| 19 | ART-META-001 | PASS | 本命/身法/心性三分支各 4 张 128 FirstParty 节点图标；EditMode 356/356、PlayMode 17/17、Validation PASS |
| 20 | ART-META-002 | PASS | 青岚风纹片/药圃生春扣/旧庭寻脉针各 1 张 2048 source master＋256 Icon；EditMode 359/359、PlayMode 17/17、Validation PASS |
| 21—27 | ART-COLLECT-001—ART-UI-005 | PENDING | 必须继续按第 3 节顺序执行，不得跳序 |

ART-CHAR-001 的初版格切因风弧跨格判定 `FAIL`；第二次针对性技术修订经透明化、连通组件归位和
左右行校正后，每格 Alpha Bounds 均保留至少 12 px 安全边，四角 Alpha=0。失败源/working 与最终源均
保留在 provenance 中。Atlas 地址为 `qinglan/character/lu-qingye/directional-animation-atlas`，Profile
地址为 `qinglan/profile/character/lu-qingye`；source/working 没有 Addressables Entry。

ART-CHAR-002 的肖像只引用已批准、项目自有的 ART-CHAR-001 源图维持角色连续性；轮廓图由已批准
Atlas 的 Down/Idle Alpha 确定性派生。肖像和轮廓地址分别为
`qinglan/character/lu-qingye/portrait` 与 `qinglan/character/lu-qingye/silhouette`；两者四角 Alpha=0，
肖像保留至少 48 px 安全边，轮廓以 `#163D45` 核心和 `#F4EFD8` 4 px 外缘保持灰阶/高对比可读。

ART-CHAR-003 为纯 FirstParty 确定性程序化资产，不使用 ImageGen、外部素材或图片参考。四档分别以
断环方钉、单弧叶片、双弧箭羽、三弧翼冠区分，除色彩外还保持环数、标记形状/数量与最高档翼冠轮廓
差异。512 final 的 Alpha≥16 覆盖率依次为 3.27%、5.41%、12.02%、19.50%，全部保留至少 40 px
安全边；地址为 `qinglan/character/lu-qingye/riding-wind/tier-0` 至 `tier-3`。

ART-ENEMY-001 对草灵、纸鹤符灵、木制剑傀、石灯守卫、鸣风铃灵、爆裂种囊分别执行一次无图片输入的
ImageGen 调用；每张 final 为 4×4、1024×1024，行序 Down/Left/Right/Up、列序
Move/AttackWindup/Hit/Death。初版色键视觉 QA 因细洋红边判定 `FAIL`，统一收边 1 px 后每格至少
12 px Alpha 安全边且检测到的洋红残留为 0。六个 Profile 使用稳定 `qinglan.enemy.*` ContentId 并
默认绑定 `down.move`；完整运行时动画驱动仍由 G3.1 最终集成关闭。

ART-AFFIX-001 为纯 FirstParty 确定性程序化资产。狂奔使用三道切向风痕与六枚箭头，结界使用分段六边
护罩与六枚锚点，分裂使用双断环、三道裂纹及两枚子体菱形，震地使用三层扁椭圆断环与八枚径向楔标；
四者不依赖颜色即可区分。四张 512 final 的 Alpha≥16 覆盖率分别为 15.87%、18.33%、13.93%、
19.12%，四角透明并保留至少 40 px 安全边；源与 final 两次生成 8/8 Hash 字节一致。正式地址为
`qinglan/enemy/affix/<frenzy|barrier|splitting|quake>/overlay`；运行时 Affix 到叠加层的映射由 G3.1
最终集成统一关闭。

ART-BOSS-001 对折枝、听风各执行一次无图片输入的 ImageGen 调用。每张 Atlas 固定 4 行×8 列，行序
Down/Left/Right/Up，列序 Move/Hit/Phase1Windup/Transition2/Phase2Windup/Transition3/Phase3Windup/
Defeated；4096×2048 working master 与 2048×1024 final 分别使用 512/256 方格。初次等距组件归位因
ImageGen 非等距留白漏格而 `FAIL` 且未输出，改用 Alpha 行谷＋每行八个最大主体从左到右排序后，64 格
均非空、四角透明、格内安全边≥12 px、洋红残留为 0；二次后处理 4/4 Hash 字节一致。两个 Profile
使用 `qinglan.enemy.boss.*` 稳定 ID 和 `down.move`；完整阶段动画与 Telegraph Overlay 由 G3.1 最终
集成及下一批 ART-BOSS-002 关闭。

ART-BOSS-002 为纯 FirstParty 确定性程序化资产，Seed 固定为 31007，不使用 ImageGen、外部素材或
图片参考。折枝三阶段分别使用横向试炼长廊/楔形排线、三枚落木目标环、分段八角阵与四根阵桩；听风
三阶段分别使用斜向冲锋走廊、破碎听风螺旋与残响菱标、交叉誓约通道与中心誓环。六张 1024 final 的
Alpha≥16 覆盖率为 12.38%—22.81%，四角透明并保留至少 60 px 安全边；两次生成 12/12 Hash 字节一致。
正式地址为 `qinglan/boss/<zhezhi|tingfeng>/phase-<1|2|3>-overlay`；阶段状态到 Overlay 的运行时映射、
材质叠加和 Overdraw 压测由 G3.1 最终集成与 G3.5 关闭。

进入 ART-SKILL-001 创建 Projectile/Area Profile 时发现，旧工具把从 1 开始的 `EntityKind` 底层值误作
零基 `enumValueIndex`，导致此前 9 个 Actor Profile 序列化为 Projectile。已改用枚举底层 `intValue`，
重写陆青野、六普通敌人和两 Boss Profile，更新三个 provenance 的实际 Hash，并在既有测试中加入
`Actor=1` 回归断言；修复后 EditMode 317/317、PlayMode 17/17、Validation PASS。该缺陷修复不增加
Manifest 完成数，ART-SKILL-001 仍按第 8 行独立提交。

ART-SKILL-001 为纯 FirstParty 确定性程序化资产，Seed 固定为 31008，不使用 ImageGen、外部素材或
图片参考。游风剑使用横剑与三道回返风弧，镇邪黄符使用切角竖符与无文字折雷纹，离火飞轮使用六段
环刃/六辐/三枚离火，听潮珠使用核心珠/双向潮弧/四滴水标，震岳印使用分段八角印/双峰/四镇石，
灵藤种使用种核/三向藤蔓/叶片与生长弧。六张 512 final 的 Alpha≥16 覆盖率为 14.38%—27.89%，
安全边≥50 px，四角透明；两次生成 12/12 Hash 字节一致。正式纹理地址为
`qinglan/skill/base/<weapon>/vfx`，Profile 地址为 `qinglan/profile/skill/base/<weapon>`；前三者为
Projectile，后三者为 Area。正式技能内容仍引用 Placeholder PresentationId，统一替换与运行时加载由
G3.1 最终集成关闭。

ART-SKILL-002 为纯 FirstParty 确定性程序化资产，Seed 固定为 31009。青岚流影剑使用三影剑/折返轨迹/
三风痕，太一镇灵符阵使用双层八角阵/八无文字符牌/聚合线，赤炉百工轮使用外八内六双环刃/八辐/
炉心，镜海潮生轮使用纵向镜核/三重对向潮弧/八相位标，山河镇界印使用四角领域/双层山河/护域弧，
地脉生春枝使用三节点地脉/中心主干/单代枝叶。六张 1024 final 的 Alpha≥16 覆盖率为
13.60%—32.56%，安全边≥63 px，四角透明；两次批准轮 12/12 Hash 字节一致。赤炉 Profile 为 Projectile，
其余五个为 Area；正式纹理/Profile 地址使用 `qinglan/skill/evolved/` 与
`qinglan/profile/skill/evolved/` 前缀。显化内容 PresentationId 替换和通用周期 VFX 请求仍由 G3.1
最终集成关闭。

ART-STATUS-001 为纯 FirstParty 确定性程序化资产，Seed 固定为 31010。燃烧、毒化、迟缓、定身、破甲、
标记和伤害免疫分别使用双焰、毒滴螺旋、断环逆向箭、四根锚根、裂盾、分段靶环与六段护盾；接触保护
和 Boss 危险区是伤害策略视觉，不新增 Gameplay Status，分别使用对向接触块/冷却环与双层危险三角。
九张 512 final 的 Alpha≥16 覆盖率为 15.53%—32.84%，安全边≥35 px，四角透明，轮廓 Bounds 全部
唯一；两次批准轮 18/18 Hash 字节一致。只有 Boss 危险区使用 P0 危险红 `#E45D45`。正式地址使用
`qinglan/status/<name>/visual` 和 `qinglan/damage-policy/<name>/visual`；Status/伤害通道到纹理的运行时
映射、材质动画与生命周期由 G3.1 最终集成关闭。

ART-PICKUP-001 对青木露、定界符、震霄雷玉、聚灵葫芦、护心玉、乘风羽分别执行一次无图片输入的
ImageGen 调用。六份服务原图均为 1254×1254 洋红键色 PNG；本地 soft matte、despill 和 12/220 阈值
去背后，确定性派生六张 256 Sprite 与六张 128 Icon。Sprite 的 Alpha≥16 覆盖率为 32.29%—61.48%，
Icon 为 30.52%—57.64%，安全边分别≥16/10 px，四角透明，洋红残留为 0，六种 Sprite Bounds 全部
唯一；二次处理 18/18 working/final Hash 字节一致。正式地址为
`qinglan/pickup/<name>/<sprite|icon>`；PickupId 到 Sprite/Icon 的运行时映射由 G3.1 最终集成关闭。

ART-RELIC-001 对断剑穗、风脉铜片、药圃种囊、听风木芯、旧庭残钟、无字试剑牌分别执行一次无图片
输入的 ImageGen 调用。六份 1254×1254 洋红键色 source 经本地 soft matte/despill 去背后，确定性派生
六张 256 Icon；Alpha≥16 覆盖率为 43.03%—53.61%，安全边≥16 px，四角透明，洋红残留和 P0 危险红
均为 0，六种 Bounds 全部唯一；二次处理 12/12 working/final Hash 字节一致。正式地址为
`qinglan/relic/<name>/icon`；RelicId 到 Icon 的运行时映射由 G3.1 最终集成关闭。

ART-MAP-001 对中央练剑场、西侧药圃、东侧藏剑廊、北侧旧山门、南侧迎客庭分别执行一次无图片输入的
ImageGen 调用。五份 1254×1254 全幅正交地表 source 被确定性归一化为 4×4 逻辑格，再打包为每区一张
4096×1024 working master 和 2048×512 final；每张 final 含 16 个唯一 256×256 Tile，完全不透明，
精确 P0 危险红为 0，逻辑格平均接缝差为 3.5992—8.6831（预算≤12）。二次处理 10/10 working/final
Hash 字节一致。正式地址为 `qinglan/map/old-court/region/<region>/tile-kit`；地图绘制、Tile 规则与正式
PresentationId 接入由 G3.1 最终集成关闭。

ART-MAP-002 对五区域分别执行一次无图片输入的 ImageGen 调用，每张 source 固定 4×4、16 个非交互
环境道具，并明确排除后续 Objective/Event/Landmark 独占物件。五份 1254×1254 洋红键色 source 经
soft matte/despill 去背、逐格 Alpha 裁切和安全边归一化后，派生每区一张 4096×1024 working master
和 2048×512 final；五张 final 共 80 个唯一 256×256 Prop，单格 Alpha≥16 覆盖率为 18.14%—47.49%，
安全边≥24 px，洋红残留和精确 P0 危险红均为 0。完整链第二次处理 15/15 Hash 字节一致。正式地址为
`qinglan/map/old-court/region/<region>/prop-set`；场景摆放、遮挡排序、碰撞和正式目录接入由 G3.1 最终
集成关闭。

ART-OBJECTIVE-001 为听风台、引风台、止衡台分别生成 Idle/Active/Complete 三态。听风台初版因底座
出现拟人面具式石雕在源级人工 QA 判定 `FAIL`，失败候选保留但不处理/寻址；重生成后使用纯几何圆环与
听风翼。引风台用方台、四向风槽和风叶展开，止衡台用八角台、对向衡板和配重归位。三份批准 source
经 soft matte/despill 去背后，派生 2048² source master、1920×640 state master 和 960×320 final；
九个 320×320 状态均唯一，安全边≥20 px，覆盖率 41.49%—63.66%，Active/Complete 的青色能量像素均
高于 Idle，洋红残留和精确 P0 危险红为 0。完整链第二次处理 12/12 Hash 字节一致。正式地址为
`qinglan/objective/wind-altar/<listen|guide|stop-balance>/state-atlas`；状态机到 Sprite 切换由 G3.1
最终集成关闭。

ART-EVENT-001 使用 mixed-source 生产：风脉暴动四叶风结、药圃复苏普通三株生长簇、旧剑共鸣三枚断剑
残片分别执行一次无图片输入的 ImageGen 调用；第一方确定性脚本再为三者添加四相风环/生长根脉/剑鸣
回声动态层。药圃初版增长环与第二版根脉射线分别因安全边为 0 在处理门禁判定 `FAIL`，回收半径和中心
后 final 安全边为 21 px。三套批准内容派生 2048² source master、2048×512 phase master 和
1024×256 final；十二个 256×256 帧均唯一，安全边≥14 px，覆盖率 20.73%—28.90%，洋红残留和精确
P0 危险红为 0，完整链第二次处理 12/12 Hash 字节一致。三个 Area Profile 使用稳定 Event ContentId，
正式地址为 `qinglan/event/<event>/phase-atlas` 和 `qinglan/profile/event/<event>`；事件状态/相位驱动由
G3.1 最终集成关闭。

ART-LANDMARK-001 对风脉旧碑、藏剑封存匣、药圃异种、断墙剑痕、迎客亭旧信分别执行一次无图片输入的
ImageGen 调用；旧信首版因封蜡出现类似字形的印记在源级人工 QA 判定 `FAIL`，候选保留但不处理/寻址，
批准重试改为无封蜡的空白纸包与素绳。五份批准 source 经 soft matte/despill 去背和 Alpha<16 归零后，
派生 2048² source master、1920×640 state master 和 960×320 final；十五个 320×320 状态均唯一，
安全边≥20 px，覆盖率 28.40%—47.93%，四角 Alpha=0，洋红残留和精确 P0 危险红均为 0。完整链两轮
20/20 Hash 字节一致。正式地址为 `qinglan/landmark/<landmark>/state-atlas`；运行时发现/领取状态到
Sprite 切换由 G3.1 最终集成关闭。

ART-HUB-001 对问脉台、藏卷楼、百器阁、万象阁分别执行无图片输入的 ImageGen 调用；问脉台首个服务
调用持续超过十四分钟且未产出任何文件，终止后按相同规格重试并取得批准源。四份 1254² source 经
soft matte/despill 去背、Alpha<16 归零和等比居中后，分别派生 2048² source master、1024 panel 与
256 icon；四张 panel 安全边≥64 px、覆盖率 40.94%—49.20%，四张 icon 安全边≥16 px、覆盖率
41.65%—49.77%，八份 final Hash 均唯一，四角 Alpha、洋红残留和精确 P0 危险红均为 0。完整链第二轮
16/16 Hash 字节一致。正式地址为 `qinglan/hub/facility/<facility>/<panel|icon>`；Facility Snapshot 到
Panel/Icon 与 Locked/Available/Visited/Updated UI 表现由 G3.1 最终集成关闭。

ART-META-001 为纯 FirstParty 确定性矢量路径栅格资产，不使用 ImageGen、外部素材、字体或图片参考。
本命、身法、心性分别使用八角圆锚、菱形三角锚、圆角方框方锚作为分支外框；12 个节点再以剑风亲和、
阈值仪、预览镜、三剑冠、足步缓冲、回息沙漏、三向路线、叠箭终端、候选镜、余量珠、行迹碑和风险
分岔区分。第 4 节点统一增加终端外冠与金色内环。初版终端外冠缩到 128 后安全边为 9 px，第二版身法
外框因描边外扩为 source 76/final 9 px，均在生成门禁判定 `FAIL`；内收后第三版 source 安全边为
95—109 px、final 为 12—13 px，覆盖率 33.51%—62.52%。12 张 final 在丢弃色相后的灰阶 Hash 仍全部
唯一，完整生成第二轮 24/24 source/final Hash 字节一致。正式地址为
`qinglan/hub/meta-node/<innate|movement|mind>/<01..04>/icon`；MetaNode PresentationId 与 Loadout UI 接入
由 G3.1 最终集成关闭。

ART-META-002 对青岚风纹片、药圃生春扣、旧庭寻脉针分别执行无图片输入的 ImageGen 调用。青岚风纹片
首版因细长尖锐轮廓易读为武器，在 source 人工 QA 判定 `FAIL`；失败候选仅作 provenance，批准重试改为
圆角梯形插片、榫接耳、安装孔与 S 形风道，不表达必定武器供给。三份批准 source 经 soft matte/despill
去背、Alpha<16 归零和等比居中后，分别派生 2048² source master 与 256² final；master 安全边均为
128 px，final 安全边均为 16 px，final 覆盖率为 47.04%—52.60%，三张图在丢弃色相后的灰阶 Hash 仍
全部唯一，四角 Alpha、洋红残留和精确 P0 危险红均为 0。完整处理链第二轮 9/9 文件 Hash 字节一致。
正式地址为 `qinglan/hub/insert/<insert>/icon`；插片装配、候选约束与效果说明由通用 Meta UI/规则层接入，
图标本身不承诺具体武器、精确唯一收藏位置或无上限恢复收益。
