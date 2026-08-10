# Codex 结果报告

- 任务：实际启动本机《剑起青岚》Demo，诊断并修复战斗中无画面问题
- 里程碑：G3.6 Release Candidate 运行时画面修复
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`30ead2c`、`35ca60d`、`9b2102e`
- 日期：2026-08-10

## 1. 实现范围

- 实际启动既有 Windows Release Player，复现并定位战斗画面不可见问题。
- 修复 Run HUD 仍保留全屏不透明页面背景和页面面板、遮挡世界相机的问题；地图详情仍保持为前景覆盖层。
- 将 5 个正式地图区域的 80 张地块子图与 80 张场景物件子图接入运行时地图表现，保留正式资源加载失败时的程序化回退。
- 在 Release Player 烟测中增加战斗世界可见性、地图地块、正式地块、正式场景物件和实际截图门禁。
- 在隔离 Player 副本中替换本次编译的托管程序集，实际打开窗口并完成 1920×1080 截图与全流程烟测。
- 未生成新的正式 Release Build；Unity Editor 的本机许可证无有效 entitlement，Unity PlayMode 与构建门禁无法执行。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs` | Run HUD 隐藏全屏页面背景/面板，地图覆盖层仅保留前景面板，并暴露世界可见状态。 |
| `Assets/Game/Infrastructure/QinglanFormalVisualLoader.cs` | 通过 Addressables 精确加载 5 个区域的地块与物件子图并管理句柄生命周期。 |
| `Assets/Game/Infrastructure/QinglanDemoRuntimeHost.cs` | 将正式地图 Sprite 集合注入地图表现配置。 |
| `Assets/Game/Infrastructure/QinglanProceduralMapFactory.cs` | 将内容地图数据与正式地块/物件复制到表现层 DTO。 |
| `Assets/Game/Presentation/ProceduralMapPresentation.cs` | 创建可见地面网格与区域物件，并保留程序化回退。 |
| `Assets/Game/Presentation/PresentationCoordinator.cs` | 暴露地图表现计数供测试门禁读取。 |
| `Assets/Game/Infrastructure/QinglanG28DevelopmentSmokeRunner.cs` | 烟测 Schema 升至 4，增加世界可见性、地图计数和截图证据。 |
| `Assets/Tests/PlayMode/QinglanG26UiInputPlayModeTests.cs` | 增加 Run HUD 与正式地图可见性断言。 |
| `Scripts/run-player-smoke.ps1` | 截图模式显示 Player 窗口，并校验新增画面门禁字段。 |
| `.github/workflows/windows-self-hosted.yml` | 本次诊断后恢复为仅 PR/手动触发；最终文件不保留临时分支 push 触发。 |
| `Docs/Reports/2026-08-10-runtime-visual-recovery.md` | 本报告。 |

## 3. 关键架构决定

- 画面资源仍由 Infrastructure 在启动阶段通过 Addressables 持有，Presentation 只接收 Sprite DTO，不改变模拟层依赖方向。
- 地图表现只读取内容拥有的边界、区域、障碍和标记，不把玩法状态硬编码到具体地图资源。
- 正式地图子图按稳定 Addressables 地址和已治理的子资源名加载；加载为空时回退为程序化地面，避免空相机清屏。
- 截图证据成为 Release Player 烟测的一部分，但仅在显式提供 `QINGLAN_G36_RELEASE_SCREENSHOT_DIR` 时生成。
- 本次未改变 Content Schema、存档格式、Tick 频率、程序集依赖方向或第三方包，因此无需新增 ADR。

## 4. 实际执行的命令

```text
Builds/WindowsRelease/AzureSword.exe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <log>
dotnet build Game.UI.csproj --no-restore
dotnet build Game.Presentation.csproj --no-restore
dotnet build Game.Infrastructure.csproj --no-restore
dotnet build Game.Tests.PlayMode.csproj --no-restore
"C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe" -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform PlayMode -testFilter Game.Tests.PlayMode.QinglanG26UiInputPlayModeTests.ActiveRunHudRevealsBattlefieldAndMapUsesOnlyAForegroundOverlay -testResults TestResults\QinglanDemo\ManualVisual\focused-playmode.xml -logFile TestResults\QinglanDemo\ManualVisual\focused-playmode.log
"C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe" -batchmode -projectPath E:\ai\free-world -runTests -testPlatform PlayMode -testFilter Game.Tests.PlayMode.QinglanG26UiInputPlayModeTests.ActiveRunHudRevealsBattlefieldAndMapUsesOnlyAForegroundOverlay -testResults TestResults\QinglanDemo\ManualVisual\focused-playmode-graphics.xml -logFile TestResults\QinglanDemo\ManualVisual\focused-playmode-graphics.log
Scripts/run-player-smoke.ps1 -Executable Builds/WindowsVisualFixProbe-c780b39/AzureSword.exe -LogPath TestResults/QinglanDemo/ManualVisual/formal-subassets-player.log -ResultPath TestResults/QinglanDemo/ManualVisual/formal-subassets-player.json -SavePath TestResults/QinglanDemo/ManualVisual/formal-subassets-save -ScreenshotPath TestResults/QinglanDemo/ManualVisual/formal-subassets-screenshots -TimeoutSeconds 120
git -c safe.directory=E:/ai/free-world commit ...
git -c safe.directory=E:/ai/free-world -c "core.sshCommand=ssh -o StrictHostKeyChecking=yes -p 443" push ssh://git@ssh.github.com:443/free-world-team/free-world.git HEAD:refs/heads/codex/qinglan-demo-implementation
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 受影响托管程序集编译 | PASS | `Game.UI`、`Game.Presentation`、`Game.Infrastructure`、`Game.Tests.PlayMode` 均为 0 错误；Infrastructure 有 15 个既有 CS0649 警告。 |
| 本机 Player 实际启动与完整烟测 | PASS | `TestResults/QinglanDemo/ManualVisual/formal-subassets-player.json`：`status=PASS`、进程退出码 0。 |
| 本机实际画面检查 | PASS | `formal-subassets-screenshots/active-gameplay.png` 为 1920×1080；角色、正式地图、物件和 Run HUD 均可见。 |
| 正式地图运行时门禁 | PASS | Player 日志为 `Map tiles=80, props=80`；结果为 432 个地块、432 个正式地块、15 个正式物件。 |
| EditMode | NOT RUN | Unity Editor 无有效许可证；本次没有取得测试运行结果。 |
| PlayMode | NOT RUN | Unity 返回 198：无 entitlement，`com.unity.editor.headless` 不可用。 |
| 完整内容验证/迁移 | NOT RUN | 需要 Unity Editor；Player 启动期内容包 1 个、定义 193 个仅属于烟测证据。 |
| 新 Release Build | NOT RUN | Unity Editor 无有效许可证，未生成新的正式构建。 |
| 性能/Soak | NOT RUN | 本次为画面恢复修复，且 Unity 门禁不可用。 |

## 6. 构建产物

- 配置：隔离视觉修复探针；既有非 Debug Release Player 外壳 + 本次 `dotnet build` 生成的托管程序集。该目录仅用于本机诊断，不是正式发布物。
- 路径：`Builds/WindowsVisualFixProbe-c780b39/AzureSword.exe`
- EXE SHA-256：`34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- 烟测 JSON SHA-256：`53D9303E17A24F265B668C8B8CE350624F870F713B19CBF83023A47B962E1BAA`
- 截图 SHA-256：`2859A95E19F16B0EED7933BDE62D65A1B0759947D01857161A2A888B4BE21AF5`
- Build Manifest：NOT RUN；未生成新的正式 Release Build Manifest。

## 7. 未执行项目

- Unity PlayMode 测试：`focused-playmode.log` 明确报告没有有效 Unity Editor license，并缺少 `com.unity.editor.headless` entitlement。
- 去掉 `-nographics` 的 PlayMode 重试得到相同许可证错误。
- 使用现有手动许可证文件刷新时得到 `TimeStamp validation failed`。
- GitHub 自托管 Unity Runner 当前离线，临时分支工作流保持 queued，不能替代本机门禁。
- 因此没有执行新 Release Build、完整内容验证、Soak 与性能门禁，以上项目均不得视为通过。

## 8. 已知限制和风险

- 当前可视验证使用隔离的“既有 Player 外壳 + 新托管程序集”，能够证明运行时代码修复有效，但不能替代从当前提交全量构建出的正式 Release。
- 截图在烟测进入战斗的初始 Tick 生成，只验证首屏地图、角色与 HUD；不代表完整战斗时长的视觉品质验收。
- 地图目前采用固定表现网格和区域物件采样；进一步的地块邻接、装饰密度与遮挡规则属于后续美术表现优化，不是本次无画面修复范围。

## 9. 未完成项

- 在有效 Unity 6 `6000.3.20f1` 许可证环境执行新增 PlayMode 测试。
- 从 `9b2102e` 之后的最终提交生成全新 Windows Release，并再次运行同一截图烟测。
- 完成 Release 构建门禁、内容验证与性能/Soak 后，才能把本次结果升级为正式候选版本通过。

## 10. 下一步前置条件

- 为本机 Unity Editor 恢复有效 entitlement，或恢复带 `[self-hosted, Windows, X64, unity]` 标签且持有许可证的 GitHub Runner。
- 使用当前分支最新提交执行 `.github/workflows/windows-self-hosted.yml` 的完整 Release Candidate 工作流。

## 11. 结论

`INCOMPLETE`

无画面问题的源代码修复与本机 Player 视觉探针均已完成并通过，但 Unity PlayMode 和全新 Release Build 因许可证环境未执行，尚不能声明正式 Release Candidate 全门禁完成。
