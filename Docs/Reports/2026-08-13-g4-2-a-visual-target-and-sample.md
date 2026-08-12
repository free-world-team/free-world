# Codex 结果报告

- 任务：基于前沿幸存者品类设计与可商用资产规则，完成《剑起青岚》G4.2-A 视觉目标、样板优化与验收包
- 里程碑：G4.2-A Visual Target and Sample
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：实现提交 `515d438`—`ea97ad1e19e8509050843be569ce2339626456a1`；本报告提交见 Git 历史
- 日期：2026-08-13

## 1. 实现范围

本阶段把用户反馈的“UI、美术、特效整体较差”拆成可执行的青瓷剑意视觉方向和一个可验证垂直切片，而不是直接扩大到全量资产替换。

- 建立青瓷剑意 Style Bible、设计令牌、概念目标图与非运行时 provenance；
- 优化战斗 HUD、三卡升级界面、设置/可访问性预览和长局构筑条密度；
- 优化中央 24 m 战斗区、玩家轮廓/边缘光、敌人暗描边、危险填充/方向、拾取物密度分组；
- 增加固定 Seed、90 秒、6 张战斗截图、7 张可访问性截图和 1 张灰阶截图的真实 Player 自动门禁；
- 修复验收夹具在 Development/Release 间因渲染位置、帧分组和候选顺序造成的分叉，最终以固定 Tick、稳定 ContentId 和高覆盖构筑运行；
- 实际构建并验收 Development 与 Windows Release Player，实际运行根启动器。

未批量替换五区、六种敌人、两个 Boss 或全套 UI；概念图只用于方向评审，不进入运行时和 Release。没有改变 Content/Save Schema、模拟规则、30 Hz Tick、第三方运行时包或资源加载后端。没有合并 `main`、打标签或创建商店提交。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Docs/ArtDirection/G42A/STYLE_BIBLE.md` | 青瓷剑意视觉原则、令牌、战斗可读性、HUD 密度和独立人类评分门槛。 |
| `Docs/ArtDirection/G42A/*` | Before 基线、概念目标、提示词与 provenance；概念图明确为非运行时/非 Release。 |
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs`、`QinglanUiTheme.cs` | 紧凑 HUD、三卡布局、设置预览、最多六个核心构筑槽与 `+N` 汇总。 |
| `Assets/Game/Presentation/QinglanPresentationTheme.cs`、`PresentationCoordinator.cs`、`EntityView.cs` | 青瓷令牌、中央区过渡、角色/敌人轮廓、危险层和密集拾取表现。 |
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs` | G4.2-A 90 秒自动门禁、固定 Tick 路线、稳定候选和高覆盖验收构筑。 |
| `Assets/Game/Infrastructure/QinglanDemoFlowController.cs` | 允许验收夹具使用固定 Run/Reward Seed，不影响普通开局。 |
| `Scripts/run-qinglan-g42-visual-acceptance.ps1` | 实际窗口、Responding、Bootstrap、结果 JSON 和 14 张截图门禁。 |
| `Assets/Tests/EditMode/QinglanG42VisualThemeTests.cs` | 视觉令牌、UI 布局、战场密度、HUD 上限与验收固定 Tick 回归。 |
| `Docs/Reports/2026-08-13-g4-2-a-visual-target-and-sample.md` | 本报告。 |

## 3. 关键架构决定

- G4.2-A 是“方向冻结 + 垂直切片 + 证据包”，不是全量美术生产；只有独立人类 Rubric 签字后才能进入 G4.2-B。
- 高密度拾取仍保留每个 Simulation-backed View，只压低重复视觉强调，不删除模拟实体或改变拾取逻辑。
- HUD 长局只显示六个核心构筑槽，其余以 `+N` 汇总；完整构筑真值仍保留在 `RunUiSnapshot`。
- 自动验收必须用固定 Tick 真值驱动导航，并按稳定 ContentId 选择高覆盖构筑；不得依赖渲染 Transform、帧率或候选数组物理顺序。
- 概念图由 ImageGen 生成，只作为非运行时目标图；正式资产仍必须逐项满足 provenance、许可证和 Hash 门禁。
- 本阶段没有产生要求 ADR 的长期架构变化。

## 4. 实际执行的命令

```text
git status --short --branch
git diff --check
git commit ...
git push origin codex/qinglan-demo-implementation
git ls-remote origin refs/heads/codex/qinglan-demo-implementation
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform All -ResultsDirectory TestResults/G42A/AcceptanceVisualPriority
.\Scripts\validate.ps1 -LogPath TestResults/G42A/Final/validation.log
.\Scripts\build-windows.ps1 -OutputPath Builds/WindowsDevelopmentG42/AzureSword.exe ...
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 -Executable Builds/WindowsDevelopmentG42/AzureSword.exe ...
.\Scripts\build-windows-release.ps1 -LogPath TestResults/G42A/Final/build-windows-release.log
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 -Executable Builds/WindowsRelease/AzureSword.exe ...
Start-Process cmd.exe /c Run-Qinglan-Demo.cmd（等待窗口、10 次 Responding、Bootstrap、Profile Hash）
Get-FileHash -Algorithm SHA256 Builds/WindowsRelease/AzureSword.exe
```

中间还真实执行并保留了失败证据：首次 HUD PlayMode 命名契约失败、一次根启动器 9/9 样本不足、以及多次 Release 验收因旧峰值门槛未达而 FAIL。修复没有降低 `Actor >= 103`、`Pickup >= 268`、`VFX >= 42` 门槛。

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| Managed/Unity 编译 | PASS | 全量 Unity Test Runner 和两种 Player Build 均完成编译。 |
| EditMode | PASS | 469/469，0 failed/skipped；`TestResults/G42A/AcceptanceVisualPriority/editmode.xml`。 |
| PlayMode | PASS | 23/23，0 failed/skipped；`TestResults/G42A/AcceptanceVisualPriority/playmode.xml`。 |
| 内容/项目验证 | PASS | 最终提交 `ea97ad1` 上 `[Project Validation] PASS`。 |
| Windows Development Build | PASS | `Builds/WindowsDevelopmentG42/AzureSword.exe`，Manifest `Succeeded`。 |
| Development 90 秒视觉门禁 | PASS | 94/94 响应；Pickup 424、Actor 201、VFX 84；JSON SHA-256 `0AB45F29E26F068BC23601D378BAD9143081D6B6D734D3FEED6266A02F7FB538`。 |
| Windows Release Build | PASS | Unity 6000.3.20f1 / StandaloneWindows64 / 非 Development / Validator PASS。 |
| Release 90 秒视觉门禁 | PASS | 94/94 响应；Pickup 351、Actor 239、VFX 90；432 地面块、65 道具、14 中央过渡、Placeholder=0、未批准资产=0。 |
| 根启动器 | PASS | 实际启动当前 `Builds/WindowsRelease/AzureSword.exe`；Bootstrap 已观察；窗口响应 10/10。 |
| 当前用户存档不变 | PASS / NOT RUN | 现有 `profile.json` Hash 前后均为 `746E75C57A9725BA6BCC3DF8248573832DAE3ED787C3534E2D41A7AEC8AA9C75`；当前账户无 `settings.json`，设置 Hash 门禁 NOT RUN，且启动前后仍不存在。 |
| 独立人类视觉 Rubric | NOT RUN | 必须由用户或其他独立人类对 Before/After、灰阶和可访问性截图评分签字。 |
| 目标 GPU / 1% Low / 长时 Soak | NOT RUN | 仍属于 QD-KI-017 和全产品发布门禁；90 秒自动门禁不能替代。 |

Release 自动验收 JSON：`TestResults/QinglanDemo/G4.2-A-Release-Final/player-90s.json`，SHA-256 `1A4E4CC16CBB2B31CB93B2B3691DF2FAC5AAEC213535217302B0CE80E94CC052`。`humanVisualSignoff=false`。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：`Builds/WindowsRelease/BuildManifest.json`，SHA-256 `029F3B6ECDF49634BC8E9C4A9FBBC0B9374C1E0CB39E4414C6EB4A7B1935C966`
- Manifest Git：`ea97ad1e19e8509050843be569ce2339626456a1`，`workingTreeClean=true`
- Release 内容：正式包 1 个、Placeholder=0、未批准资产=0、Release Validator=`PASS`

## 7. 未执行项目

- 独立人类最终视听签字：`NOT RUN`。自动截图检查与 Codex 人工查看不能替代独立签字。
- 商业权利和 Steam AI 披露签字：`NOT RUN`，不属于本阶段授权。
- 当前 G4 正式表现代码上的目标 GPU、1% Low、长时间性能和最低规格物理机器认证：`NOT RUN`。
- 当前 Windows 账户不存在 `settings.json`，所以设置文件 SHA-256 前后比较：`NOT RUN`；没有把文件缺失描述为通过。
- 未合并 `main`、未打 Release 标签、未创建商店提交。

## 8. 已知限制和风险

- G4.2-A 的自动部分完成不等于 G4.2 视觉方向已获独立人类批准，也不等于商业 Release GO。
- 当前战斗画面仍使用既有正式资产组合；全量五区、敌人、Boss 和 UI 的一致性生产尚未开始。
- 90 秒门禁是可读性/覆盖门禁，不是目标 GPU 性能认证；性能数据只能视为本机短时样本。
- 概念图不得直接作为游戏资源或第三方正式资产输入；G4.2-B 的每个正式资产仍需 provenance、许可证和 Hash。
- QD-KI-003、QD-KI-014、QD-KI-017 仍未关闭。

## 9. 未完成项

- 独立人类按 `Docs/ArtDirection/G42A/STYLE_BIBLE.md` Rubric 对 Before、最终 0/15/30/45/60/90 秒、灰阶和可访问性截图评分并签字。
- 获批后才可创建 G4.2-B 单里程碑任务，按资产清单分批替换和复测。
- 全产品外部门禁仍需商业/法律、最低规格和目标 GPU 性能证据。

## 10. 下一步前置条件

- 人工运行：双击根目录 `Run-Qinglan-Demo.cmd`。
- 人工评审证据：`TestResults/QinglanDemo/G4.2-A-Release-Final/Screenshots`；基线与目标图位于 `Docs/ArtDirection/G42A`。
- G4.2-B 开始条件：独立人类总分至少 80/100，且每维不低于该维满分的 60%，并留下签字记录。
- 未经用户新授权，不合并 `main`、不打 Release 标签、不创建商店提交。

## 11. 结论

`INCOMPLETE`

G4.2-A 的实现、自动测试、项目验证、双 Player 90 秒门禁、最终 Release 和根启动器验证均已完成；但该里程碑强制要求的独立人类视觉 Rubric 尚为 `NOT RUN`，因此不能宣称 G4.2-A COMPLETE，也不能开始全量 G4.2-B 或表述为商业 Release GO。
