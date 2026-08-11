# 02 G0—G3 交付路线与分支结构

## 1. 单工作包纪律

每次只实施下表一个工作包。分支默认使用 `codex/gN-<work-package>`；只有前一依赖包的审查、
合并和结果报告完成后才能创建下一分支。不得在一个 PR 中同时扩 Schema、批量做正式资产并完成
Release 调优。

## 2. G0 方案冻结

| 顺序 | 分支建议 | 交付物 | 退出门禁 |
|---:|---|---|---|
| G0.1 | `codex/g0-demo-structure` | 本文档集、追踪矩阵、ID 草案 | 文档检查 PASS |
| G0.2 | `codex/g0-demo-cr-review` | CR-01—CR-11 决策包 | 每项 Accepted/Rejected/Deferred |
| G0.3 | `codex/g0-demo-schema-contracts` | 获批 ADR、Schema/API/Save 迁移与测试计划 | 架构审查 PASS |
| G0.4 | `codex/g0-demo-asset-plan` | 资产/音频/字体/本地化生产清单与预算 | provenance 流程可执行 |

若 G0.2 拒绝某项能力，必须同步修改 Demo 完成定义或找到不改变体验承诺的现有模块组合；不能静默
采用近似行为。

## 3. G1 数据切片

| 顺序 | 分支建议 | 模块 | 主要验证 |
|---:|---|---|---|
| G1.1 | `codex/g1-demo-approved-modules` | 获批通用模块/Schema | API Freeze、EditMode、Validation、性能短测 |
| G1.2 | `codex/g1-demo-character-combat` | M02、M03 | 乘风状态机、属性/状态、固定 Seed |
| G1.3 | `codex/g1-demo-weapons` | M04 | 六技能等级、预览、ProcDepth、清理 |
| G1.4 | `codex/g1-demo-build-content` | M05 | 六心诀、Offers、Synergy/Evolution 可达性 |
| G1.5 | `codex/g1-demo-enemies` | M07 | 六敌人、四词缀、行为与攻击技能 |
| G1.6 | `codex/g1-demo-encounter` | M09 | 12 分钟时间轴、并发上限、固定 Seed |
| G1.7 | `codex/g1-demo-pack-gate` | M05、M15 | Reward Choice 适配器、完整 Pack Bake、双语占位、Development Build |

G1 只使用程序化 Placeholder。正式角色、Logo、字体、音频和品牌素材不得混入数据切片。

## 4. G2 可玩切片

| 顺序 | 分支建议 | 模块 | 主要验证 |
|---:|---|---|---|
| G2.1 | `codex/g2-demo-map-runtime` | M08 | 五区、三风脉台、三事件、五地标 |
| G2.2 | `codex/g2-demo-bosses` | M10 | 折枝、听风三阶段、目标修正 |
| G2.3 | `codex/g2-demo-rewards` | M06 | 灵物、奇物、显化宝匣、首通奖励 |
| G2.4 | `codex/g2-demo-game-flow` | M01 | 标题→Run→结算→据点→再次出发 |
| G2.5 | `codex/g2-demo-meta-save` | M11、M14 | 行脉、嵌片、收藏、故事、Meta 校验与幂等结算事务 |
| G2.6 | `codex/g2-demo-ui-input` | M12 | 键鼠/手柄、页面、HUD、可访问性 |
| G2.7 | `codex/g2-demo-placeholder-polish` | M13 | 程序化表现、池、预警和音频占位 |
| G2.8 | `codex/g2-demo-vertical-slice-gate` | 全模块 | PlayMode、Development Build、可读性评审 |

G2.6 已在统一实现分支交付，实际证据见 `21_G2_6_UI_INPUT_ACCESSIBILITY.md` 与
`Docs/Reports/2026-08-09-g2-6-ui-input-accessibility.md`；G2.7 必须继续复用其单一 Canvas、输入命令和
只读 `RunUiSnapshot`，不得建立第二套 UI 或玩法真值。

G2.7 已在统一实现分支交付，实际证据见 `22_G2_7_PLACEHOLDER_PRESENTATION_POLISH.md` 与
`Docs/Reports/2026-08-09-g2-7-placeholder-presentation-polish.md`；G2.8 只整合和审查完整垂直切片，
不得借门禁工作包提前导入 G3 未完成 provenance/许可的正式资源。

G2.8 已在统一实现分支交付，实际证据见 `23_G2_8_VERTICAL_SLICE_GATE.md` 与
`Docs/Reports/2026-08-09-g2-8-vertical-slice-gate.md`；G2 全部工作包退出门禁已通过。下一工作包只进入
G3.1 正式视觉资产与 provenance/Addressables，不提前制作 G3.2 音频或 G3.3 字体/正文。

G3.1 已在统一实现分支完成 27/27 Manifest ART 批次和最终运行时集成，实际证据见
`24_G3_1_FORMAL_VISUAL_ASSETS.md` 与
`Docs/Reports/2026-08-10-g3-1-formal-visual-final-integration.md`。下一工作包只进入 G3.2 正式音频，
不得提前制作 G3.3 字体/正文或执行 G3.4 数值冻结。

G3.2 已在统一实现分支完成 9/9 Manifest AUDIO 批次、104 Clip Catalog、Mixer/Router 与最终构建门禁，
实际证据见 `25_G3_2_FORMAL_AUDIO.md` 与
`Docs/Reports/2026-08-10-g3-2-formal-audio-final-integration.md`。下一工作包只进入 G3.3 正式字体、
简中/英文/Pseudo 与正文可读性，不得提前执行 G3.4 数值冻结。

G3.3 已在统一实现分支完成三套正式 TMP 字体、806 个双语 Key、三表运行时路由、Pseudo 全角映射、
字形预热与 150% Windows Player 布局门禁，实际证据见
`26_G3_3_FORMAL_FONT_LOCALIZATION.md` 与
`Docs/Reports/2026-08-10-g3-3-formal-font-localization-final-integration.md`。下一工作包只进入 G3.4
数值冻结与 Seed 矩阵，不提前执行 G3.5 性能优化或 G3.6 Release Candidate。

G3.4 已在统一实现分支完成 Pack `0.10.0` 数值冻结：15 局矩阵取得 12 胜 3 负，三路线胜场为
`3/4/5`，三 Golden、三失败探针和 18 奇物兼容全部通过，实际证据见
`27_G3_4_BALANCE_FREEZE.md` 与
`Docs/Reports/2026-08-10-g3-4-balance-freeze-final-integration.md`。当前工作包只进入 G3.5 目标
硬件 CPU/GPU/GC/池容量取证，不提前执行 G3.6 Release Candidate。

G3.5 已在参考目标机完成 54,000 Tick CPU 与 1080p 30 分钟 GPU Target：平均 59.992 FPS、1% Low
59.891、GPU p99 2.163 ms、Tick p99 12.760 ms、稳态 0 B/GC 0，完整回归和 Development Player
均通过；2,000 Enemy 非阻断扩展实际记录为 `FAIL` 并明确 CPU 容量拐点。实际证据见
`28_G3_5_TARGET_HARDWARE_PERFORMANCE.md` 与
`Docs/Reports/2026-08-10-g3-5-target-hardware-performance-final-integration.md`。下一工作包只进入 G3.6
Release Candidate。

G3.6 已按 `29_G3_6_RELEASE_CANDIDATE.md` 冻结：现有 M10 纯框架 Release Smoke 不作为 Demo 发布
证据；候选必须只装载正式青岚 Catalog 和正式 Addressables，并完成 Null Platform Release Player、
Manifest、合规、干净克隆与 DOD-01—10 当前提交证据。RC2 `9984bcc` 的自动测试、Release、Player、
合规、干净 Checkout 与 CI 已全部 `PASS`；独立人工/法律签字和最低规格实机认证仍为 `NOT_RUN`，故
当前发布决定是 `NO-GO / INCOMPLETE`，不进入后续发布或商店工作。

用户人工检查确认 G3.6 的“正式资产存在/加载”没有形成武器、怪物、动画、特效、可点击 UI 与 2.5D
空间的成品闭环。新增 G4.0 单里程碑按 `30_G4_0_2_5D_PRESENTATION_COMPLETION.md` 修复该缺口；在
DOD-01—10 全部 `PASS` 前，G3.6 历史自动证据不能用于宣称 Demo 表现完成。

## 5. G3 发布候选

| 顺序 | 分支建议 | 交付物 | 退出门禁 |
|---:|---|---|---|
| G3.1 | `codex/g3-demo-art-import` | 正式角色、敌人、地图、UI、VFX Profile | provenance＋Addressables PASS |
| G3.2 | `codex/g3-demo-audio-import` | 音乐、环境、机制和危险提示 | 许可证＋混音可读性 PASS |
| G3.3 | `codex/g3-demo-localization` | zh-Hans/en 正文、字体与伪本地化 | Locale/裁切/字体 PASS |
| G3.4 | `codex/g3-demo-balance` | 数值冻结与 Seed 矩阵 | 三构筑与失败率目标 PASS |
| G3.5 | `codex/g3-demo-performance` | 目标硬件 CPU/GPU/GC/池证据 | 1080p 60、1% Low 警报审查 |
| G3.6 | `codex/g3-demo-release-candidate` | Release Build、Smoke、Manifest、合规包 | DOD-01—10 全部 PASS |

## 5.1 G4 人工验收修复

| 顺序 | 分支建议 | 交付物 | 退出门禁 |
|---:|---|---|---|
| G4.0 | `codex/qinglan-demo-implementation` | 2.5D 战场、动画、武器、正式 VFX、成品 UI、60 秒证据 | G4.0 DOD-01—10 全部 PASS |

## 6. 每个工作包固定交付

1. 范围与明确非范围；
2. 修改文件、内容 ID 和资产来源；
3. 关键决定及 ADR/CR；
4. 实际命令；
5. `PASS/FAIL/NOT RUN` 证据表；
6. 生成物路径与 Hash；
7. 风险、未执行项和下一工作包前置条件。

## 7. 回滚原则

- 内容回滚：移除未发布 Catalog 条目并重 Bake；发布 ID 不复用；
- Schema 回滚：保留旧读取器与迁移 Fixture，不能只降低版本号；
- 公共 API 回滚：恢复 Freeze Hash 前先验证下游源码/二进制兼容；
- 资产回滚：Addressables 清单与 provenance 同步回滚；
- 平衡回滚：只修改内容数值和版本，不更改确定性随机流协议。
