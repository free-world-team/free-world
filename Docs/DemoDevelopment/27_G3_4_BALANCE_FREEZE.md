# G3.4 数值冻结与 Seed 矩阵

## 1. 目标

在不改变 30 Hz Tick、确定性随机流协议、Content Schema 6、存档 Schema 3 和程序集依赖方向的前提下，
完成《剑起青岚》Demo 普通难度的首个正式数值冻结。G3.4 必须用真实技能伤害、真实敌人伤害和真实玩家生命
完成三条目标构筑矩阵，并证明失败仍然可达；G2.8 的百万生命、持续治疗和诊断伤害只保留为流程集成证据，
不得进入本里程碑的平衡结论。

本工作包只处理内容数值、自动玩家、Golden、测试和冻结清单，不提前进行 G3.5 目标硬件性能优化或 G3.6
Release Candidate 封板。

## 2. 三条目标构筑

| 路线 | 核心技能 | 核心心诀 | 显化 | 优先奇物方向 |
|---|---|---|---|---|
| 移动御剑 | `qinglan.skill.weapon.yufeng_sword` | `qinglan.passive.treading_wind` | `qinglan.evolution.qinglan_flowing_shadow_sword` | 移动、回返、次级剑气 |
| 符阵爆发 | `qinglan.skill.weapon.yellow_talisman` | `qinglan.passive.clear_mind` | `qinglan.evolution.taiyi_spirit_sealing_array` | 稳定多目标、控制、风险换伤 |
| 草木铺场 | `qinglan.skill.weapon.spirit_vine_seed` | `qinglan.passive.spirit_gathering` | `qinglan.evolution.earth_vein_spring_branch` | 生长、恢复、持续区域 |

自动玩家只允许提交移动、交互、升级、奖励选择等真实玩家命令。允许只读采集 World 遥测；禁止直接写
Actor/Health/Stat/Build Store，禁止注入伤害、治疗、经验、拾取范围或技能，禁止瞬移和跳过 Boss 阶段。

## 3. 固定 Seed

三组 Seed 在实现前冻结。每组前五项进入统计矩阵，第一项同时是该路线 Golden：

| 路线 | Seed 基值 | 矩阵 Seed |
|---|---|---|
| 移动御剑 | `0x47333453574F5200` | `...01`、`...02`、`...03`、`...04`、`...05` |
| 符阵爆发 | `0x47333454414C4900` | `...01`、`...02`、`...03`、`...04`、`...05` |
| 草木铺场 | `0x4733344649454C00` | `...01`、`...02`、`...03`、`...04`、`...05` |

Golden 完整值分别为 `0x47333453574F5201`、`0x47333454414C4901`、
`0x4733344649454C01`。不得在看到结果后换 Seed 规避失败；设计变更需要在报告中说明原因并更新冻结版本。

## 4. 自动玩家策略

- 走位：围绕当前目标作带切向量的风筝移动；Boss 优先于普通敌人，交互目标优先于巡航。
- 导航：只使用地图查询和 `RunSession.SetMoveDirection`，卡路不得直接改位置。
- 升级：核心技能 → 核心心诀 → 对应显化 → 路线同标签能力 → 生存能力 → 稳定 ContentId 次序。
- 候选：统计矩阵不使用无限重掷或放逐；无目标候选时选当前得分最高项。
- 奖励：按路线奇物亲和度选择，平分时使用稳定 ContentId 次序。
- 失败探针：不移动、不交互，升级请求只执行 Skip，奖励使用稳定首项；不得让 Pending Choice 停住时钟。

## 5. 统计门槛

### 5.1 三构筑矩阵

15 局必须全部跑到真实胜利或玩家死亡结果，不得由诊断代码主动 `End`：

- 总胜局 `10—13/15`，即胜率 `66.7%—86.7%`、失败率 `13.3%—33.3%`；
- 每条路线胜局 `3—5/5`，防止总体结果掩盖单路线失衡；
- 三条 Golden 必须胜利、击败两个 Boss，并在 `21,600—22,950` Tick 内结束；
- Golden 必须形成核心技能 8 级、核心心诀 5 级和对应显化；
- 任一矩阵失败局不得由无效 Handle、异常、容量耗尽或导航写 Store 导致；
- 三条路线 Decision/Build Checksum 必须互异。

### 5.2 确定性

三条 Golden 各重放一次，逐项比较 Completed Tick、胜负、等级、Build、击杀、Boss、目标、奖励、
Spawn/Objective/Boss/Decision/Combined Checksum。任一差异即 `FAIL`。

### 5.3 失败可达

三条 Golden Seed 各执行一局失败探针，要求 `3/3` 玩家死亡、`0/3` 击败最终 Boss。失败原因必须是
`PlayerDefeated`，不得超时、手动结束或异常退出。

### 5.4 六奇物兼容

六件战斗奇物分别与三条目标构筑组成 `3×6=18` 个兼容用例。每个用例必须通过真实 Reward/Relic Runtime
授予并运行最少 1,800 Tick，证明输出可解析、效果可执行、清理后无实体或 Handle 残留。矩阵中六件奇物还必须
各至少被真实候选并选择一次；不能用空效果或 fallback 冒充覆盖。

## 6. 冻结产物

- `QinglanG34BalanceCommand`：统一运行统计矩阵、Golden 重放、失败探针和奇物覆盖，输出机器可读 JSON。
- EditMode：自动玩家禁作弊契约、评分稳定性、阈值判定、Golden 等价和冻结清单校验。
- `g3-4-balance-freeze.json`：记录 Pack 版本、Baked Catalog SHA-256、受控定义/字段和结果证据 SHA-256。
- 三条逐局 Golden JSON：只在经记录的设计数值变化后更新，不得用来覆盖非预期回归。
- 最终报告：列出所有数值迭代，包括发生过的 `FAIL`、根因和最终候选证据。

## 7. 执行顺序

| 步骤 | 交付 | 提交边界 |
|---:|---|---|
| 1 | 本设计、矩阵和门槛冻结 | 文档单独提交并 Push |
| 2 | 无作弊自动玩家、报告模型和 EditMode 契约 | 代码/测试单独提交并 Push |
| 3 | 真实基线、内容数值迭代和 Pack 版本更新 | 每轮有效数值冻结分别提交并 Push |
| 4 | Golden、六奇物兼容与冻结清单 | 证据/清单单独提交并 Push |
| 5 | 全量测试、内容验证、Development Build、Player Smoke 与结果报告 | 最终集成单独提交并 Push |

## 8. 强制验证

| 检查 | G3.4 最低要求 |
|---|---|
| 聚焦 EditMode | 自动玩家、判定、确定性、奇物兼容全部 `PASS` |
| 平衡矩阵 | 15 局胜负/失败率、3 Golden 重放、3 失败探针全部 `PASS` |
| 全量 EditMode | `PASS` |
| 全量 PlayMode | `PASS` |
| 内容验证/迁移 | `PASS` |
| Windows Development Build | `PASS` |
| 构建后 Player Smoke | `PASS` |

任一强制项为 `FAIL` 或 `NOT RUN` 时，G3.4 结论只能是 `INCOMPLETE`，不得开始 G3.5。

## 9. 最终冻结结果（2026-08-10）

G3.4 最终结论为 `PASS`。Demo Pack 冻结为 `0.10.0`，Baked Content Hash 为
`8900fedffde84c2d014c260d50bff1833a1a98f378a4b22ac08ea4a3ec40d21f`。

| 指标 | 结果 |
|---|---:|
| 15 局矩阵 | 12 胜 / 3 负 |
| 路线胜局 | 移动御剑 3、符阵爆发 4、草木铺场 5 |
| Golden 重放 | 3/3 确定性一致 |
| 失败探针 | 3/3 `PlayerDefeated` |
| 奇物兼容 | 18/18 PASS，六件均在矩阵中被选择 |
| 无效 Handle | 0 |

有效候选迭代没有被隐藏：最终 Boss 生命 50 的候选虽通过平衡矩阵，但使 G2.8 听风阶段覆盖退化为 `3/7`；
2600 的可读性候选保留三阶段，却只产生 2 胜并造成多条路线超时；最终 800 候选同时得到 12/15 目标胜率和
G2.8 四路线三阶段覆盖，因此作为正式冻结值。浮点 Preview Golden 使用 `0.0001` 容差，双跑 Summary 仍保持
精确相等。

## 10. 最终证据

- 冻结清单：[g3-4-balance-freeze.json](Assets/G3.4/g3-4-balance-freeze.json)
- 完整矩阵：[g3-4-balance-report.json](Assets/G3.4/g3-4-balance-report.json)
- Golden：[移动御剑](Assets/G3.4/golden-moving-sword.json)、[符阵爆发](Assets/G3.4/golden-talisman-burst.json)、
  [草木铺场](Assets/G3.4/golden-living-field.json)
- 全量 EditMode：451/451 `PASS`
- 全量 PlayMode：20/20 `PASS`
- 项目治理验证、Windows x64 Development Build、独立 Player Smoke：全部 `PASS`

G3.4 门禁已关闭，可以按单里程碑纪律进入 G3.5。
