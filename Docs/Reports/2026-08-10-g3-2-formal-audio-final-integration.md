# Codex 结果报告

- 任务：完成 G3.2 正式音频 Final Integration
- 里程碑：G3.2 正式音频、混音与 Addressables
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：本报告所在提交
- 日期：2026-08-10

## 1. 实现范围

将九个已批准批次的 104 个 Runtime Clip 接入正式 Catalog、Addressables 生命周期、AudioMixer Snapshot、
集中式优先级 Router、探索/高压/Boss Stem 和 UI 输入反馈。扩展 Release 缺失阻断、EditMode/PlayMode、
Windows Development Build 与构建后 Player Smoke 证据。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `FormalAudioCatalog.cs` | 104 Clip、语义 Cue、Boss Stem 与 Mixer Snapshot 契约 |
| `QinglanFormalAudioLoader.cs` | Startup Addressables Handle Owner 与释放 |
| `PresentationEffects.cs` | 32 Source、8 Stem、8 P0 预留、Cooldown、Duck、DSP 调度 |
| `PresentationCoordinator.cs` | 正式 Catalog 注入、Run/Boss/Mix/UI Cue 路由 |
| `QinglanDemoRuntimeHost.cs` | 正式音频加载、UI 输入 Cue 与销毁顺序 |
| `QinglanG32FormalAudioIntegration.cs` | Catalog/Mixer 生成、验证与 Addressables Build CLI |
| `formal-audio-catalog.asset`、`qinglan-demo.mixer` | 104 Binding、Master、四 Snapshot |
| `QinglanG32FormalAudioIntegrationTests.cs` | Catalog、Loader、预算、Duck、Stem、Cooldown |
| `QinglanG32FormalAudioPlayModeTests.cs` | 实际 PlayMode 加载、DSP 调度、P0 路由与释放 |
| `QinglanG28DevelopmentSmokeRunner.cs` | Player 正式音频与 32/8/8 取证 |

## 3. 关键决定

- Catalog 是 104 个正式 Clip 的唯一间接引用入口；Simulation 不持有 AudioClip 或 Addressables 句柄。
- 总 AudioSource 容量固定 32，其中 8 个 Stem、24 个瞬态；瞬态中的 8 个只供 P0/机制请求。
- Stem 采用同一 DSP 起点批量调度：环境 1、探索/战斗/高压 4、Boss 三阶段 3。
- `OrdinaryDuckLinear=0.5011872`（-6 dB），`StoryEnvironmentDuckLinear=0.6309574`（-4 dB）。
- Snapshot 与手动音量乘区并存：Snapshot 表达状态，显式乘区保证用户独立音量和测试可重复性。
- Development 缺 Catalog 使用测试音并告警；项目验证阻断任何缺 Catalog、地址、Mixer 或 Snapshot 的构建。

## 4. 实际执行的命令

```text
Unity.exe ... QinglanG32FormalAudioIntegration.Run
Unity.exe ... QinglanG32FormalAudioIntegrationTests
Unity.exe ... QinglanG27PresentationPolishTests
Unity.exe ... QinglanG32FormalAudioPlayModeTests
Unity.exe ... ProjectValidationCommand.Run
Unity.exe ... -runTests -testPlatform EditMode
Unity.exe ... -runTests -testPlatform PlayMode
Unity.exe ... QinglanG32AddressablesBuildCommand.Run
Unity.exe ... WindowsDevelopmentBuild.BuildFromCommandLine（首次与重试）
powershell.exe ... Scripts/run-qinglan-g28-player-smoke.ps1
git diff --check
```

## 5. 测试和构建结果

| 检查 | 结果 | 证据 |
|---|---|---|
| Formal Audio Focused EditMode | PASS | `formal-audio-editmode.xml`；5 / 5 |
| G2.7 Pool 回归 | PASS | `g27-audio-regression.xml`；7 / 7 |
| Formal Audio PlayMode | PASS | `formal-audio-playmode.xml`；1 / 1 |
| 完整 EditMode | PASS | `full-editmode.xml`；419 / 419 |
| 完整 PlayMode | PASS | `full-playmode.xml`；18 / 18 |
| 项目验证 | PASS | `formal-audio-validation.log` |
| Addressables Build | PASS | `addressables-build.log`；674 Locations |
| Windows Development Build 首次 | FAIL | `windows-development-build.log`；Win32 IO 1224 临时映射锁 |
| Windows Development Build 重试 | PASS | `windows-development-build-retry.log`；`[M0 Build] PASS` |
| Player Smoke | PASS | `player-smoke-final.json`；正式音频加载/Cue 路由与完整流程 |

## 6. 构建产物

- 配置：Windows x64 Development / Unity 6000.3.20f1。
- Player：`Builds/WindowsDevelopment/AzureSword.exe`。
- Addressables：`Library/com.unity.addressables/aa/Windows/settings.json`。

## 7. 未执行项目及原因

- Release Build/Release Smoke：NOT RUN；按执行顺序属于 G3.6，当前仍有 G3.3—G3.5 前置门禁。
- 主观听音与目标声卡响度测量：NOT RUN；自动化已验证格式、峰值/RMS/DC、路由和混音系数，目标硬件听音归入 G3.5。

## 8. 已知限制和风险

- 四个 Snapshot 当前以状态切换为主，精确 Duck 同时由可测试的 Router 乘区保证；后续若在 Mixer 内增加复杂效果链，
  必须保持 -6 dB / -4 dB 契约与用户音量独立性。
- 首次 Development Build 遇到已退出 Unity 遗留的 Windows 文件映射锁；确认无残留进程后重试通过。

## 9. 未完成项

- 无 G3.2 遗留实现。

## 10. 下一步前置条件

- G3.2 独立提交/Push 后进入 G3.3 正式字体、简中/英文/Pseudo 与正文可读性。

## 11. 结论

`PASS`。九批正式音频、104 Clip Catalog、Mixer/Router、Addressables 生命周期、完整测试、实际构建和
Player Smoke 均已通过；G3.2 可以关闭。
