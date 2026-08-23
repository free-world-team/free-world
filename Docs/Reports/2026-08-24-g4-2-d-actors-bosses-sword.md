# Codex 结果报告

- 任务：按 32 号生产规范完成 G4.2-D 角色、六敌、两 Boss 与游风剑重制
- 里程碑：G4.2-D
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`99ef8195f44869735064b12cd52a9edfa61b6d93`
- 日期：2026-08-24

## 1. 实现范围

在现有集中方向动画和对象池架构内完成运行时角色强化：玩家、普通敌、Boss 的 1080p 目标高度分别约为
110 px、91 px、198 px；两 Boss 额外加载四方向三阶段共 24 张正式帧，并由只读 `RunUiSnapshot.BossPhase`
投影到 ActorView。游风剑增加稳定 Socket、移动/攻击/收招四态、攻击增亮/放大与 72° 剑弧 Trail。

新增 Actor 池获取、命中与扩容指标。没有新增逐敌人 `Update`/`Animator`，没有改变 Simulation Tick、伤害、
碰撞或技能真值，也没有导入第三方资源。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Presentation/DirectionalEntityPresentation.cs` | 可选的四方向三阶段 Boss 帧集合和计数 |
| `Assets/Game/Infrastructure/QinglanFormalVisualLoader.cs` | 两 Boss 共 24 张阶段帧 Addressables 加载 |
| `Assets/Game/Presentation/EntityViews.cs` | Boss 阶段投影、游风剑 Socket/四态/攻击 Trail、池化清理 |
| `Assets/Game/Presentation/ViewPools.cs` | 角色比例 Token、共享 Trail 材质、复用/扩容指标 |
| `Assets/Game/Presentation/QinglanPresentationTheme.cs` | 角色屏幕比例和武器动作 Token |
| `Assets/Game/Presentation/PresentationCoordinator.cs` | Boss Phase、武器和 Actor Pool 只读指标 |
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs` | G4.2-D Player 指标采样和硬门禁 |
| `Assets/Tests/EditMode/M7PresentationUiInputTests.cs` | Boss 三阶段、武器四态、Trail 与解绑清理测试 |
| `Assets/Tests/EditMode/QinglanG31FormalVisualIntegrationTests.cs` | 24 张 Boss 阶段帧真实加载测试 |
| `Assets/Tests/EditMode/QinglanG42VisualThemeTests.cs` | 1080p 角色比例与武器 Token 测试 |
| `Assets/Tests/PlayMode/QinglanG27PresentationPolishPlayModeTests.cs` | 真实 Run 武器和池化观察测试 |
| `Scripts/run-qinglan-g42-visual-acceptance.ps1` | 外层角色/Boss/武器/池化门禁 |

## 3. 关键架构决定

- 继续使用一个集中 `PresentationCoordinator` 驱动所有 ActorView；阶段和武器反应均为 Presentation-only。
- 复用现有已审批正式图集；阶段帧由 Addressables 在启动时一次加载，不在高频路径解析地址。
- 角色比例和武器动作数值进入 `QinglanPresentationTheme`，不散落在具体 View。
- Assembly、Schema、存档、Tick、渲染后端和第三方依赖未改变，未新增 ADR。

## 4. 实际执行的命令

```text
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.M7PresentationUiInputTests.BossPhasesAndYufengSwordUseFormalStatefulPooledPresentation
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.QinglanG31FormalVisualIntegrationTests.AddressableLoaderOwnsAndReleasesTheFormalCatalogHandle
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform PlayMode -testFilter Game.Tests.PlayMode.QinglanG27PresentationPolishPlayModeTests.RealRunBuildsMapDistinctPlayerSilhouetteAndBoundedPresentationPools
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform PlayMode
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.ProjectValidationCommand.Run
Scripts/build-windows-release.ps1 -OutputPath Builds/WindowsRelease/AzureSword.exe
Scripts/run-qinglan-g42-visual-acceptance.ps1 -Executable Builds/WindowsRelease/AzureSword.exe -ScreenWidth 1920 -ScreenHeight 1080
git commit -m "feat(g4.2-d): strengthen actors bosses and sword"
git push origin codex/qinglan-demo-implementation
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| Boss/游风剑聚焦 EditMode | PASS | 1/1；阶段帧、四态、Trail、解绑清理 |
| 正式阶段图 Addressables | PASS | 9 组方向图、2 组 Boss、24 张阶段帧 |
| G4.2 Theme/比例 | PASS | 8/8；1080p 玩家/敌/Boss 目标区间 |
| 真实 Run 聚焦 PlayMode | PASS | 1/1；观察到攻击剑弧与池指标 |
| 全量 EditMode | PASS | `472/472`；包含六敌、两 Boss、玩家正式图集轮廓/Alpha/方向 QA |
| 全量 PlayMode | PASS | `24/24` |
| 内容验证 | PASS | `[Project Validation] PASS` |
| Release Build | PASS | Manifest `Succeeded`、`workingTreeClean=true` |
| 90 秒 Player | PASS | 92.69 秒，自动 Gate=`true`，攻击 Trail 最大 1 |
| 高压池化 | PASS | 最大 239 Actor；Acquire=915、PoolHit=684、Expansion=231、Created=239 |
| 独立人类灰度/25%/一秒识别 | NOT RUN | 自动轮廓 QA 不能替代独立人类识别签字 |

Player GPU 326 个样本的平均值为 2.74 ms、P99 为 6.90 ms（RTX 3060 Ti / D3D12）。本次 90 秒运行
未到 360 秒首 Boss，自然 Boss 阶段仍留给 G4.2-F 真实 12 分钟局；阶段装配和真实资源加载由 EditMode 覆盖。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：SHA-256 `7FA81A08BF5957838568F118CE2F06F5D404A467D80925CDA835781B7AB4CC2A`

## 7. 未执行项目

- 独立人类灰度、25% 缩略图和一秒闪现识别：`NOT RUN`。
- 真实 12 分钟局中的折枝、听风三阶段自然演出：`NOT RUN`，属于 G4.2-F。
- 最低规格物理机器和物理 4K：`NOT RUN`。

## 8. 已知限制和风险

- 六敌和两 Boss 的正式资产有自动轮廓差异证据，但主观识别速度仍需独立人类判断。
- Boss 稳态阶段使用现有 `phase-N-windup` 正式帧；阶段切换完整节奏和音画协同在 G4.2-E/F 继续验证。
- 90 秒验收使用 3.25× Simulation Scale，不作为最终 12 分钟真实时长证据。

## 9. 未完成项

- G4.2-E 五阶段 VFX、危险反馈、Reduce Motion 和音频混音。
- G4.2-F 真实 12 分钟 Player、目标硬件、法律与独立人类最终签字。

## 10. 下一步前置条件

- 依据用户免阶段审核的持续执行指令进入 G4.2-E；未执行的人类门禁继续保持 `NOT RUN`。

## 11. 结论

`INCOMPLETE`。

G4.2-D 工程实现、资源加载、完整测试、验证、Release Build、Player 和高压池化均已通过；独立人类识别与
真实 Boss 自然流程未执行，因此不能写为 `COMPLETE`。工程执行继续 G4.2-E。
