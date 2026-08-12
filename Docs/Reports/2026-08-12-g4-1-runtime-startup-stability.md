# Codex 结果报告

- 任务：修复双击/现有存档启动无响应，并审计、修复相关游戏内运行问题
- 里程碑：G4.1 Runtime Startup Stability
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：实现提交 `6f7a084`、`9df1536`、`1999d08`、`5dcad76f17193327952f34858c51e6632adb6bac`；本报告所在提交见 Git 历史
- 日期：2026-08-12

## 1. 实现范围

本修复任务以用户本机真实 `zh-Hans` 设置和现有 Profile 为输入，复现并关闭双击后窗口消失/无响应问题，同时审计同一路径上的存档、本地化、启动器选择和同步等待风险。

- 新增“正常参数 + 现有存档”独立 Player 启动门禁，连续采样窗口响应，并在退出后核对设置/Profile SHA-256 未变化；
- 修复低频本地小型 JSON 存档在 Unity 同步 Composition Boundary 上使用异步文件 I/O 后被同步等待造成的主线程停滞；
- 修复启动 `Awake` 阶段立即把持久化 `zh-Hans` 写入 `LocalizationSettings.SelectedLocale`，触发同步本地化重初始化并卡住首帧的问题；
- 修复根目录启动器优先选择被 `.gitignore` 排除的历史视觉探针，导致用户启动旧 Player 的问题；
- 审计其余同步等待、启动日志、Player 生命周期、正式视觉/音频/字体/本地化、战斗、VFX、存档提交和 Hub/Restart 流程；
- 重新执行全量测试、内容验证、干净 Windows Release 构建、隔离存档完整 Player 流程、本机现有存档正常启动和根启动器实际启动。

本任务没有改变模拟规则、Content/Save Schema、稳定 ID、30 Hz Tick、第三方包或正式资产。没有合并 `main`、打标签或创建商店提交。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Scripts/run-existing-save-startup-smoke.ps1` | 新增正常启动现有存档门禁：Bootstrap、响应窗口连续采样和保存文件 Hash 不变检查。 |
| `Assets/Game/Infrastructure/LocalFileSaveStorage.cs` | 低频本地小型 JSON 读写改为接口内同步完成，保留 `ValueTask`、校验和、备份、取消和 `Flush(true)` 契约。 |
| `Assets/Game/Infrastructure/QinglanDemoRuntimeHost.cs` | 持久化语言延迟到第一帧 `Update` 应用，避免 Bootstrap/Awake 内同步重初始化。 |
| `Assets/Game/Infrastructure/M7RuntimeHost.cs` | 旧/通用 Host 同步采用相同的首帧语言应用策略。 |
| `Assets/Tests/EditMode/M8SaveLocalizationPlatformTests.cs` | 增加现有本地文档读写在 Unity Composition Boundary 内同步完成的回归。 |
| `Assets/Tests/PlayMode/M8SaveLocalizationFlowPlayModeTests.cs` | 增加持久化中文后二次 Bootstrap 不阻塞且中文标题生效的回归。 |
| `Run-Qinglan-Demo.cmd` | 只选择当前 `Builds/WindowsRelease/AzureSword.exe`，不再优先历史探针目录。 |
| `README.md` | 对齐根启动器的当前 Release 选择和人工运行说明。 |
| `Docs/KNOWN_ISSUES.md`、`Docs/MASTER_PLAN.md`、`Docs/EXECUTION_LOG.md` | 登记三个根因、验证证据和 G4.1 完成状态。 |
| `Docs/Reports/2026-08-12-g4-1-runtime-startup-stability.md` | 本报告。 |

## 3. 关键架构决定

- `ILocalSaveStorage` 继续暴露异步兼容的 `ValueTask` 接口，但本地 0.6—1.5 KB JSON 文件在调用线程内完成读写；这条路径只位于标题、设置、开局和结算等低频边界，不进入固定 Tick。
- 持久化 Locale 仍由设置真值驱动，但必须等 Unity 完成首轮 Bootstrap 并进入第一帧后再切换；运行中用户主动切换 Locale 的既有行为不变。
- 根启动器只允许选择当前正式 Release 路径；不存在时明确报错并提示先构建，不能静默回退到被忽略的历史产物。
- 没有改变存档格式/迁移策略、程序集依赖、模拟 Tick、资源后端或第三方依赖，因此无需新增 ADR。

## 4. 实际执行的命令

```text
git status --short
dotnet build Game.Infrastructure.csproj --no-restore
dotnet build Game.Tests.PlayMode.csproj --no-restore
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform EditMode -TestFilter M8SaveLocalizationTests
.\Scripts\run-existing-save-startup-smoke.ps1 -Executable Builds/WindowsRelease/AzureSword.exe -SavePath C:\Users\18321\AppData\LocalLow\DefaultCompany\AzureSword\Saves ...（旧 Release 基线：FAIL）
.\Scripts\run-existing-save-startup-smoke.ps1 ...（英文默认设置、用户 Profile、用户 zh-Hans 设置组合隔离）
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform PlayMode -TestFilter M8SaveLocalizationFlowPlayModeTests
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform EditMode -ResultsDirectory TestResults/QinglanDemo/G4.1/FullEditModeFinal
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform PlayMode -ResultsDirectory TestResults/QinglanDemo/G4.1/FullPlayModeFinal
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\validate.ps1 -LogPath TestResults/QinglanDemo/G4.1/validation-final.log
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\build-windows-release.ps1 -LogPath TestResults/QinglanDemo/G4.1/build-release-final.log -EvidenceRoot TestResults/QinglanDemo/G4.1/BuildEvidenceFinal
.\Scripts\run-player-smoke.ps1 -Executable Builds/WindowsRelease/AzureSword.exe -LogPath TestResults/QinglanDemo/G4.1/release-player-final.log -ResultPath TestResults/QinglanDemo/G4.1/release-player-final.json -SavePath TestResults/QinglanDemo/G4.1/release-player-save-final
.\Scripts\run-existing-save-startup-smoke.ps1 -Executable Builds/WindowsRelease/AzureSword.exe -SavePath C:\Users\18321\AppData\LocalLow\DefaultCompany\AzureSword\Saves -LogPath TestResults/QinglanDemo/G4.1/existing-save-startup-final.log -ResultPath TestResults/QinglanDemo/G4.1/existing-save-startup-final.json
Start-Process cmd.exe /c Run-Qinglan-Demo.cmd（等待窗口后连续 10 次响应采样并核对实际 EXE 路径/存档 Hash）
rg -n -i "exception|error|assert|failed" TestResults/QinglanDemo/G4.1/*.log
git push ssh://git@ssh.github.com:443/free-world-team/free-world.git HEAD:codex/qinglan-demo-implementation
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 原问题复现 | PASS | 旧 Release + 本机现有存档：15.6 秒内未观察到 Bootstrap，稳定窗口样本 0；设置/Profile Hash 未变化。 |
| 根因隔离 | PASS | 默认英文设置 + 用户 Profile=`PASS`；用户 `zh-Hans` 设置 + 当前 Profile=`FAIL`；证明第二根因位于启动 Locale 应用而非 Profile 数据。 |
| Managed 编译 | PASS | `Game.Infrastructure` 与 `Game.Tests.PlayMode` 0 error；仅 Infrastructure 既有 DTO warning。 |
| Focused EditMode | PASS | 存档修复后 M8 专项 10/10，0 skipped。 |
| Focused PlayMode | PASS | 持久化中文二次启动专项 2/2，0 skipped。 |
| 全量 EditMode | PASS | 463/463，0 failed/skipped；XML SHA-256 `98DCAFC4429634353308BEBA6C25B9919DF87CB5F03EA1AEF42CC650824988A8`。 |
| 全量 PlayMode | PASS | 23/23，0 failed/skipped；XML SHA-256 `A93E3B73E9D26AA1B2056B3D35072B5002078D5A98CA83BCD1EF235CED3B1405`。 |
| 内容验证 | PASS | `validation-final.log`；SHA-256 `DFB8896F2C6DFDD30AA10B3A0C88CCF6246FD1131E876CC8E5AF91E8DC1090F9`。 |
| Windows Release Build | PASS | Unity 6000.3.20f1 / StandaloneWindows64 / 非 Development；Manifest `Succeeded`、干净 `5dcad76`、Placeholder=0、未批准资产=0。 |
| 隔离存档完整 Player 流程 | PASS | Title、角色/地图/Loadout、战斗世界、暂停、升级、结算保存、Hub、Restart 全部走到；正式视觉/音频/字体/本地化均加载。 |
| 本机现有存档正常启动 | PASS | 9.098 秒，Bootstrap 已观察，10/10 响应窗口采样；设置/Profile SHA-256 前后相同。 |
| 根启动器实际启动 | PASS | 实际路径为当前 `Builds/WindowsRelease/AzureSword.exe`；等待窗口后 10/10 响应，Bootstrap 完成，存档 Hash 未变化。 |
| 日志异常审计 | PASS | Player 日志无运行时 Exception/Assert；D3D12 info queue 提示在所有成功 Player 中均非致命。Unity LicenseClient 离线提示未阻止 Validation/Build exit 0。 |
| 性能/Soak | NOT RUN | 本任务修改低频启动/存档/本地化边界，不进入高频模拟；沿用 QD-KI-017，发布前仍需当前正式表现代码的目标硬件长时复测。 |

隔离 Player 还实际记录：战斗世界可见、432 个正式地面块、65 个正式场景道具、32 次 VFX、16 个 AudioSource 创建，保存提交成功且 Run Recovery 已清理。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development / NullPlatformFacade / Offline Required
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：`Builds/WindowsRelease/BuildManifest.json`，SHA-256 `F11109AB73762F0137038319B23334CEFB2EAD308544F9FADB07C3F0D75703B9`
- Manifest Git：`5dcad76f17193327952f34858c51e6632adb6bac`，`workingTreeClean=true`
- Release 内容：`qinglan.pack.demo` 0.10.0，193 definitions，Placeholder=0，未批准资产=0，Release Validator=`PASS`

## 7. 未执行项目

- G4.1 专用 30 分钟 Soak、目标 GPU、1% Low 和最低规格物理机器测试：`NOT RUN`。本修复不在固定 Tick 或渲染热点；这些继续属于既有发布门禁，不能由启动稳定性测试替代。
- 独立人类最终视听签字、商业权利/Steam AI 披露签字：`NOT RUN`，不属于本修复授权范围。
- Windows 桌面自动化插件初始化因本机 Codex 目录权限返回 `EPERM`；改用实际 GUI Player 进程、真实窗口句柄/响应采样、Player 日志和存档 Hash 完成验证。没有把插件未运行描述为通过。
- GitHub PR、`main` 合并和 Release 标签未执行；用户只授权当前分支逐步提交与 Push。

## 8. 已知限制和风险

- G4.1 完成只关闭启动无响应与相关运行时问题，不等于商业 Release `GO`；QD-KI-003、QD-KI-014、QD-KI-017 和最低规格物理机器证据仍阻止发布。
- Unity 在本机 D3D12 Player 输出 `failed to query info queue interface (0x80004002)`；当前所有 Player 门禁仍正常创建画面、加载 Bootstrap 并退出 0。若未来出现显卡相关崩溃，应在对应硬件上以 D3D11/D3D12 对照复现，不能把本次非致命提示外推为全部 GPU 已认证。
- 本地存档当前是小型 JSON 文档；若未来引入大体积快照，需重新设计后台序列化/写入和主线程回调边界，不能直接扩大本次同步 I/O 策略。

## 9. 未完成项

- 本次启动稳定性修复强制项：无。
- 全产品发布项：独立人类/法律签字、最低规格认证、当前 G4 正式表现代码的目标硬件长时性能复测仍未完成。

## 10. 下一步前置条件

- 人工进入游戏请双击仓库根目录 `Run-Qinglan-Demo.cmd`；它只启动 `Builds/WindowsRelease/AzureSword.exe`。
- 若进入 Release/Steam/商店工作，必须先关闭 QD-KI-003、QD-KI-014、QD-KI-017，并取得最低规格物理机器证据。
- 未经用户新授权，不合并 `main`、不打 Release 标签、不创建商店提交。

## 11. 结论

`COMPLETE`

现有中文存档和根目录启动器均已在最终 Release 上实际启动并持续响应；全量 463 EditMode、23 PlayMode、Validation、干净 Release Build、完整 Player 游戏流程和存档不变门禁全部 `PASS`。该结论只关闭 G4.1 启动稳定性修复，不覆盖仍为 `NOT RUN` 的全产品外部 Release 门禁。
