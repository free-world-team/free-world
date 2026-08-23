# Codex 结果报告

- 任务：完成 G4.2-F 真实 12 分钟 1× Release Player 验收、性能/可访问性门禁及问题修复
- 里程碑：G4.2-F
- 分支：`codex/qinglan-demo-implementation`
- 候选实现 Commit：`9cfc2ae94909ce74a7ff9519e81ac955917b51aa`
- 日期：2026-08-24

## 1. 实现范围

新增仅由 `-qinglanG42FinalAcceptance` 启用的 720 秒、30 Hz、1× Release Player 验收。运行使用固定种子和正常战斗规则，不注入无敌、治疗、伤害或强制通关；自动选择升级/奖励并持续路径移动，要求自然覆盖 Boss 三阶段、正式角色/敌人/武器/地图、五段式技能效果、音频优先级、低动态替代与 17 张截图。

最终验收记录 Windows 窗口响应、平均 FPS、1% Low、GPU P99、Draw Call/SetPass/Triangle Recorder、托管/总内存增长和 GC 次数。排查并修复了验收器重复观察导致的扰动、后台非焦点 Player 限速、最终结果时长读取，以及 200% 字号下 HUD 状态栏换行溢出。

没有改变模拟 Tick、Content Schema、存档格式、程序集方向、正式资源来源或第三方依赖，因此没有新增 ADR。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs` | 12 分钟 1× 驱动、进度心跳、Boss/性能/内存/截图硬门禁、后台自动验收 60 FPS、观察器降频 |
| `Assets/Game/Infrastructure/QinglanDemoFlowController.cs` | 安全暴露冻结后的最终结果，使自然结束仍能读取真实时长 |
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs` | 150%/200% 字号下运行状态栏自适应横向扩展 |
| `Assets/Tests/EditMode/QinglanG42VisualThemeTests.cs` | 720 秒、御风 4/4、200% 字号 HUD 溢出回归断言 |
| `Scripts/run-qinglan-g42-final-acceptance.ps1` | Release Player 启动、900 秒超时、窗口响应、JSON/截图/性能硬校验 |
| `ValidationReports/G42F*/validation.log` | 各修复节点的项目验证证据 |
| `ValidationReports/G42F-final/*` | 最终 Player JSON、日志和 17 张截图 SHA-256 清单 |

## 3. 关键架构决定

- 最终验收只调用每渲染帧一次 `TickRuntime(frameElapsed)`，固定 Tick 仍由 `SimulationClock` 在最多 4 Tick 的预算内追赶，避免验收器改变正式表现层负载。
- 自动验收模式设置 `runInBackground=true`、目标 60 FPS、VSync=0，防止脚本启动的非焦点窗口被 Windows/Unity 限速；普通用户启动配置不改变。
- 累计身份集合每 5 帧采样一次，帧时/GPU/渲染 Recorder 仍逐帧记录；验收观察本身不再制造主要负载。
- 200% 字号保留真实字号，扩展 HUD 状态栏可用宽度，不使用自动缩小字体规避无障碍要求。

## 4. 实际执行的命令

```text
Scripts/run-qinglan-g35-performance.ps1 -Mode Quick ... -QuickEnemies 260 -QuickProjectiles 18 -QuickPickups 404 -QuickVfx 90
Scripts/test.ps1 -Platform All -ResultsDirectory TestResults/QinglanDemo/G4.2-F-monitoring
Scripts/validate.ps1 -LogPath ValidationReports/G42F-monitoring/validation.log
git commit -m "fix(g4.2-f): reduce acceptance observer effect" && git push
Scripts/test.ps1 -Platform All -ResultsDirectory TestResults/QinglanDemo/G4.2-F-background-fix
Scripts/validate.ps1 -LogPath ValidationReports/G42F-background-fix/validation.log
git commit -m "fix(g4.2-f): prevent background player throttling" && git push
Scripts/run-qinglan-g42-final-acceptance.ps1 ...G4.2-F-final-c3d3694...    # FAIL：200% RunStatusLabel 溢出
Scripts/test.ps1 -Platform EditMode -ResultsDirectory TestResults/QinglanDemo/G4.2-F-ui-fix
Scripts/test.ps1 -Platform PlayMode -ResultsDirectory TestResults/QinglanDemo/G4.2-F-ui-fix          # FAIL：2 项本地化卡片偶发溢出
Scripts/test.ps1 -Platform PlayMode -ResultsDirectory TestResults/QinglanDemo/G4.2-F-ui-fix-retry    # PASS 24/24
Scripts/validate.ps1 -LogPath ValidationReports/G42F-ui-overflow-fix/validation.log
git commit -m "fix(g4.2-f): prevent 200 percent hud overflow" && git push
Scripts/build-windows-release.ps1 ...
Scripts/run-qinglan-g42-visual-acceptance.ps1 ...G4.2-F-ui-fix...                                 # PASS
Scripts/run-qinglan-g42-final-acceptance.ps1 ...G4.2-F-final-9cfc2ae...                           # PASS
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 / EditMode | PASS | `475/475` |
| PlayMode | PASS | 最终重跑 `24/24`；前一次 `22/24` 后原样重跑通过，波动保留为风险 |
| 内容/来源/发布验证 | PASS | `ValidationReports/G42F-ui-overflow-fix/validation.log` 含 `[Project Validation] PASS` |
| Windows Release Build | PASS | Unity 6000.3.20f1；Manifest `Succeeded`、正式包 1、Placeholder 0、未批准资产 0 |
| 90 秒 G4.2 视觉门禁 | PASS | 3.25×；200% 溢出 False；8 张无障碍图；关键 VFX 丢失 0 |
| 12 分钟 1× Release Player | PASS | 21600 Tick；723.93 秒墙钟；724.0 秒 HUD 时长；窗口响应 `716/716` |
| Boss 自然阶段 | PASS | Boss View=1；最大阶段索引=2；阶段观察样本=1326 |
| 正式演出/压力 | PASS | 最大 View=593、Actor=151、Pickup=433、VFX=132；五段完成=3951 |
| 关键请求零丢失 | PASS | Critical VFX=0、Critical Staged VFX=0、Critical Audio=0 |
| 帧率 | PASS | 平均 59.94 FPS；1% Low 59.50 FPS；43144 帧样本 |
| GPU | PASS | RTX 3060 Ti / D3D12；P99 4.239 ms（门槛 33.34 ms） |
| 内存/GC | PASS | 托管增长 18,837,504 B；最终总内存 250,684,169 B；Gen2=29 |
| 截图/可访问性 | PASS | 8 时间点 + 8 无障碍 + 1 灰度；`accessibilityTextOverflowObserved=false` |
| Codex Computer Use 前台点击 | NOT RUN | 宿主 node_repl 启动连续失败：`windows sandbox failed: helper_unknown_error: setup refresh had errors` |
| 独立人类视听/无障碍签字 | NOT RUN | 自动代理和自动截图不能替代独立人类签字 |
| 物理最低规格/物理 4K | NOT RUN | 当前机器为 i7-12700F / RTX 3060 Ti / 1080p |
| 法律/商业权利签字 | NOT RUN | 需要有权限的独立责任人签字 |

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development
- 启动路径：`E:\ai\free-world\Builds\WindowsRelease\AzureSword.exe`
- Executable SHA-256：`34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest SHA-256：`81838D3836C48851DDE998214036926597DE96D4445FCC4551B565FBFCEF09DC`
- Final Result SHA-256：`1BC47A6B8451DBE03C19722F6F20487072A7F429F24875C21DBB8789BD3BA17C`
- 最终证据：`ValidationReports/G42F-final/final-acceptance.json`

## 7. 未执行项目

- Codex Computer Use 前台逐项点击：`NOT RUN`，原因是 Codex Windows 沙箱辅助进程本身无法启动；不等同于游戏无响应。真实 Release Player 已完成 716 次窗口响应采样与 12 分钟自动实跑。
- 物理最低规格、物理 4K、独立人类视听/无障碍和法律签字：`NOT RUN`，需要仓库外的设备或独立责任主体。

## 8. 已知限制和风险

- PlayMode 一次运行中两项既有本地化卡片描述溢出失败，原样重跑 24/24；存在测试环境初始化/分辨率波动风险，发布前应在 CI 固定分辨率再跑一次。
- Release 下 `GC Allocated In Frame` ProfilerRecorder 不可用，因此最终门禁使用 GC collection 次数、Mono Used 和 Total Allocated Memory 的 12 分钟增长；热路径零分配另由性能/自动测试覆盖。
- 当前数据不能外推为最低规格或 4K 认证。

## 9. 未完成项

- 外部独立人类视听/无障碍签字、最低规格物理机、物理 4K、法律/商业签字。
- Codex Computer Use 前台点击需宿主沙箱辅助进程恢复后补跑。

## 10. 下一步前置条件

- 恢复 Codex Windows sandbox helper 后可直接补跑前台点击，不需要代码变更。
- 完整商业 Release 仍需外部最低规格、4K、独立人类与法律签字。

## 11. 结论

`INCOMPLETE`。

G4.2-F 工程实现、全量测试、项目验证、Release Build、90 秒视觉门禁和 12 分钟 1× 本机 Player 门禁全部通过；但外部强制签字/物理设备门禁及 Codex Computer Use 前台点击尚为 `NOT RUN`，不能把完整商业发布认证宣称为 `COMPLETE`。