# 29 G3.6 Release Candidate

## 1. 范围

G3.6 只关闭《剑起青岚》Demo 的发布候选门禁：正式内容输入、Windows x64 Release Player、离线
Null Platform 闭环、Manifest、合规审计、干净克隆和 DOD-01—10 汇总。它不新增玩法、正式资产或平衡
数值，也不把 G3.5 的 2,000 敌人扩展压力 `FAIL` 改写为通过。

现有 `WindowsReleaseBuild` 生成的 M10 临时场景只运行 60 Tick 纯框架 Smoke，不能作为 Demo Release
Player。G3.6 必须让非 Development Player 启动真实 `GameBootstrapper`、正式青岚 Catalog、正式
Addressables、输入、UI、本地化、存档与 Null Platform。

## 2. 候选边界

| 边界 | G3.6 固定规则 |
|---|---|
| Unity | `6000.3.20f1`，Windows x64，非 Development |
| 运行时内容 | 只装载 `qinglan.pack.demo` `0.10.0` 的正式运行时 Catalog |
| Addressables | 只允许实际 Release 输入；任何 Placeholder 路径或 `placeholder`/`development-only` 标签阻断 |
| 平台 | `NullPlatformFacade`；离线启动、游玩、保存和退出不得依赖 Steam/网络 |
| 场景 | 构建期生成可审计的正式 Demo Scene；不得引用 Test Pack 或 Placeholder Scene/Prefab/Sprite |
| 正式资产 | G3.1—G3.3 已批准的 Visual/Audio/Localization/Font 输入，Hash 和 provenance 必须复验 |
| 性能 | 沿用 G3.5 目标机 Target PASS；2,000 敌人压力 `FAIL` 保留为非阻断已知限制 |
| 候选来源 | Manifest 记录候选 Commit、分支、Tag、工作树清洁状态与完整输入 Hash |

正式运行时 Catalog 是 G3.4 冻结作者内容的确定性 Bake 结果副本。作者资产仍保留在开发用
`Assets/GameAssets/Placeholder`，但不得成为 Release Scene、Addressables 或 Player 的依赖；不能靠改名
掩盖 Placeholder，Release Gate 必须检查实际输入依赖和标签。

## 3. 实施顺序

1. 新增正式运行时 Catalog，并让 Release Build 只装配该 Catalog 与真实 Bootstrap；
2. 在排除开发组后显式构建 Addressables，再构建非 Development Player；
3. 运行 Release 专用 UI/输入/设置/本地化/存档/返回据点/再次出发 Smoke，要求退出码 `0`；
4. 扩展 Manifest，记录正式 Pack、验证结果、关键配置与证据 Hash；
5. 生成合规清单和 DOD-01—10 机器可读汇总；
6. 在当前候选 Commit 运行全量本地门禁；
7. 在独立干净克隆重跑 Test→Validation→Performance→Build→Player；
8. 只有实际 CI Job 成功才写 `PASS`，Runner 不可用或未启动写 `NOT RUN`；
9. 汇总 GO/NO-GO、阻断项、已知限制、人工/法律评审与发布后监控。

每一步单独提交并 Push；实现修复会使受影响的旧证据失效，必须重跑。

## 4. Release Build 契约

构建器必须同时满足：

- `BuildOptions.None`，`Debug.isDebugBuild == false`；
- Release Scene 依赖扫描 `PASS`；
- Addressables 实际纳入条目 Placeholder 数为 `0`；
- 正式 Catalog Pack 数为 `1`，Pack ID 为 `qinglan.pack.demo`，版本和 Hash 与冻结输入一致；
- provenance、第三方登记、本地化 Key、API Freeze、Save Schema 验证均为 `PASS`；
- 构建结束恢复临时 Scene、Addressables Include 状态和生成的 `link.xml`；
- 构建失败、缺 Manifest、缺 EXE 或任一结果不一致时返回非零。

`BuildManifest.json` 至少记录：Git Commit/Branch/Tag/clean、Unity、`manifest.json` 与
`packages-lock.json` Hash、Addressables Hash、Content/Save Schema、正式 Pack 版本/Content Hash/Catalog
Hash、Placeholder/未批准资产计数、测试/验证/性能状态与证据 Hash、Release Validator、平台后端、EXE
SHA-256、UTC 和 Windows x64 配置。

## 5. Release Player 契约

自动 Player Smoke 通过正常公开 UI/Application 命令覆盖：

```text
标题 → 角色 → 地图/装配 → 开始 Run → 暂停/恢复 → 升级选择
→ 胜利结算 → 原子保存 → 据点 → 再次出发
```

同时断言：

- 非 Development、Null Platform、正式 Pack `1` 个且无测试 Pack；
- 正式视觉、音频、字体、简中/英文/Pseudo、本地化与 150% 布局均就绪；
- 设置与可访问性生效，输入 Owner 唯一；
- Run 退出后 View 清零，VFX/Audio 池不越界；
- 保存文件可读取，首通提交成功，离线运行不访问远程平台；
- 结果 JSON 为 `PASS`、日志含唯一 PASS 标记、进程退出码为 `0`。

12 分钟 Encounter、两 Boss、三路线与重复确定性由当前候选重新运行 G2.8/G3.4 自动矩阵证明；Player
Smoke 只负责验证真实构建中的端到端页面、资源、平台和持久化装配，不用墙钟等待 12 分钟替代模拟证据。

## 6. DOD-01—10 关闭矩阵

| DOD | G3.6 当前候选强制证据 |
|---|---|
| DOD-01 | 全量 PlayMode XML、Release Player JSON/日志、完整闭环字段 |
| DOD-02 | 四结算/恢复 EditMode 与 PlayMode、Release 首通 Save 样本 |
| DOD-03 | G3.4 三路线 15 Seed 报告、Golden 与预览 Hash |
| DOD-04 | 18 个“构筑×奇物”组合报告和内容验证 |
| DOD-05 | 地标/事件/奖励事务测试与 G2.8 Vertical Slice 报告 |
| DOD-06 | 8 组风脉台 Boss 参数测试和 Player Boss 路径证据 |
| DOD-07 | 行脉/嵌片容量、互斥、重置、UI 和 Save 测试 |
| DOD-08 | 十次生命周期、Handle/View/Input 清理、迁移与恢复测试 |
| DOD-09 | G3.5 1080p 30 分钟 Target JSON、可读性评审；2,000 敌人扩展保留 `FAIL` |
| DOD-10 | 全量 Test/Validation/Release Build/Player/Manifest/合规/干净克隆/CI 状态 |

机器汇总只能输出 `PASS`、`FAIL`、`NOT RUN`。DOD-01—10 任一为 `FAIL` 或 `NOT RUN` 时，Demo 状态
只能是 `INCOMPLETE`，发布结论只能是 `NO-GO`。

## 7. 合规与人工评审

自动审计覆盖正式资产 provenance 完整性、SHA-256、Addressables 路由、第三方登记和许可证文件、
Placeholder、空本地化 Key、Package/Project 版本与 API Freeze。以下仍需在候选报告中明确责任人与实际
结果，不得用自动测试代替：

- AI/第一方资产商业权利和 Steam AI 披露复核；
- Noto CJK OFL 随包义务与商店法律文本复核；
- 简中/英文叙事、危险可读性、三构筑决策差异和音频遮蔽人工评审；
- 最低规格机器认证；当前只有 i7-12700F/RTX 3060 Ti 参考目标机证据。

人工/法律评审必须使用 `G3_6_MANUAL_REVIEW_GUIDE.md` 的 Schema 2 表、证据 Hash 和校验器；候选汇总只接受
与候选 Commit 匹配的 `manual-review-validation.json=PASS`，不再直接信任手填顶层状态。

最低规格认证必须使用 `G3_6_MINIMUM_SPEC_CERTIFICATION_GUIDE.md` 的本机 CIM/WMI 采集与 30 分钟一键流程。
候选汇总只接受 Schema 2 `minimum-spec-review.json` 与 Hash 匹配的
`minimum-spec-validation.json=PASS`；高配参考目标机、虚拟机、远程适配器或单独手填 `PASS` 均无效。

## 8. 测试与退出门禁

最低实际执行：聚焦 EditMode、全量 EditMode、全量 PlayMode、项目验证、G2.8 Vertical Slice、G3.4
Balance、G3.5 CPU 与正式 GPU 证据 Hash 复核、Release Build、Release Player、干净克隆完整门禁。
CI 需真实自托管 Runner Job；仅存在 YAML 为 `NOT RUN`。

G3.6 只有在 DOD-01—10 全部 `PASS`、Release Manifest 与 Player 属于同一候选 Commit、无 Release
阻断项、候选分支已 Push 后才可关闭。若外部人工/法律/最低规格或 CI 无法取得实际结果，必须报告
`NO-GO / INCOMPLETE`，不能用用户概括授权伪造签字或硬件证据。

## 9. RC2 实际结果（2026-08-10）

- 候选：`9984bcc5582bd827372768993953645d754cc463` / `qinglan-demo-g3.6-rc2`。
- 自托管 GitHub Actions Run `31370035860`：`PASS`；Clean Checkout、Unity 版本、460 EditMode、
  20 PlayMode、Validation、Vertical Slice、Balance、CPU、Compliance、Development/Release Build、
  Release Player、源树清洁和产物上传全部成功。
- Release：非 Development、Null Platform、一个正式 Pack、193 Definitions、Placeholder 0、未批准资产 0。
- DOD-01—08：`PASS`。
- 独立人工/法律签字：`NOT_RUN`；最低规格物理机器认证：`NOT_RUN`。
- DOD-09—10：`NOT_RUN`；最终决定：`NO-GO / INCOMPLETE`。

完整机器证据见 `Docs/DemoDevelopment/Assets/G3.6/Final`。首次 CI Run `31367990349` 仅在 Checkout
因 `curl 56 connection reset` 失败，未运行任何 Unity/代码门禁；失败历史保留在 `ci-summary.json`。
