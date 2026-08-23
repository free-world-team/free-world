# Codex 结果报告

- 任务：按 32 号生产规范完成 G4.2-E 五段式 VFX、战斗反馈、音频压力与低动态替代
- 里程碑：G4.2-E
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`c01116ce3d92ebb8d7cc692d0b6d089dc727aadd`
- 日期：2026-08-24

## 1. 实现范围

在既有集中 `PresentationCoordinator` 和有界池架构上新增固定容量五段式技能表现调度：
Anticipation、Launch、Travel、Impact、Residue。六个主技能从稳定 `ContentId` 派生非颜色形状、尺度和节奏签名；
P0 危险序列在容量压力下抢占低优先级序列或与既有 P0 合并，不写入模拟状态。

补齐 35 ms Presentation-only Hit Stop、0.12 幅度上限镜头冲击、真实拾取反馈、Boss 阶段反馈、按优先级
VFX/音频丢弃指标，以及关闭 Screen Shake 后的小尺度、短时、低位移 VFX、角色低摆幅和禁用武器/弹体 Trail。
没有新增逐对象 `Update`、第三方包、Schema、存档字段或模拟 Tick 改动。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Presentation/StagedPresentationEffects.cs` | 固定容量五阶段调度、稳定非颜色签名、优先级抢占/合并 |
| `Assets/Game/Presentation/PresentationCoordinator.cs` | 五阶段接线、拾取/命中/Boss、镜头预算、压力与无障碍指标 |
| `Assets/Game/Presentation/EntityViews.cs` | 35 ms 表现停顿、低动态摆幅与 Trail 替代 |
| `Assets/Game/Presentation/PresentationEffects.cs` | 音频按优先级丢弃计数 |
| `Assets/Game/Presentation/PresentationRequests.cs` | Presentation-only Pickup 请求 |
| `Assets/Game/Presentation/ViewPools.cs` | 把低动态设置投影到所有复用 View |
| `Assets/Game/Infrastructure/M7RuntimeHost.cs`、`QinglanDemoRuntimeHost.cs` | 消费统一的受限镜头冲击 |
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs` | G4.2-E 指标、低动态实跑与硬门禁 |
| `Assets/Tests/EditMode/QinglanG42ProductionEffectsTests.cs` | 六技能签名、五阶段、低动态和容量 1 P0 极限测试 |
| `Assets/Tests/EditMode/QinglanG27PresentationPolishTests.cs` | 音频普通层可降级/P0 零丢失断言 |
| `ValidationReports/G42E*/validation.log` | 本机完整项目验证证据 |

## 3. 关键架构决定

- 五阶段由单一固定数组调度并发往共享 `VfxRequestPool`，不创建每特效 Behaviour。
- 只在表现层冻结 View 35 ms；Simulation、伤害、碰撞、Fixed Tick 继续推进。
- `CriticalDanger` 满载时抢占较低优先级，只有同级 P0 时合并，不允许走普通丢弃分支。
- Screen Shake 关闭同时作为 Reduce Motion 信号；保留轮廓、方向和五阶段语义，减少运动而非删除信息。
- Pickup 反馈只接受玩家 2 单位内的 Pickup Removed，避免把远处生命周期清理表现成奖励。
- Assembly、Schema、存档、Tick、渲染后端和第三方依赖未改变，未新增 ADR。

## 4. 实际执行的命令

```text
Scripts/test.ps1 -Platform EditMode -ResultsDirectory TestResults/G42EFinalEditMode
Scripts/test.ps1 -Platform PlayMode -ResultsDirectory TestResults/G42EFinalPlayMode
Scripts/validate.ps1 -ResultsDirectory ValidationReports/G42E        # FAIL：脚本无此参数
Scripts/validate.ps1 -LogPath ValidationReports/G42E-final/validation.log
git commit -m "feat(g4.2-e): stage combat effects and feedback"
git push origin codex/qinglan-demo-implementation
Scripts/build-windows-release.ps1 -LogPath TestResults/G42E/build-windows-release.log -EvidenceRoot TestResults/QinglanDemo/G42E-release
Scripts/run-qinglan-g42-visual-acceptance.ps1 -Executable Builds/WindowsRelease/AzureSword.exe -ResultPath TestResults/QinglanDemo/G4.2-E/player-90s.json
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 / 全量 EditMode | PASS | `475/475` |
| 全量 PlayMode | PASS | `24/24` |
| 内容验证 | PASS | `[Project Validation] PASS`；首次命令参数错误后使用正确 `-LogPath` 重跑 |
| Release Build | PASS | Manifest `Succeeded`、`workingTreeClean=true`、正式包 1、占位 0、未批准 0 |
| 90 秒 Release Player | PASS | 自动 Gate=`true`；窗口响应 `106/106`；15 张截图 |
| 五阶段完整性 | PASS | 515 组开始/完成；每阶段 515 次；序列丢失 0 |
| P0 零丢失 | PASS | Critical VFX=0、Critical Staged Sequence=0、Critical Audio=0 |
| 反馈覆盖 | PASS | Hit=1113、Death=714、Pickup=95、Hit Stop=1113、Camera Impulse=947 |
| Reduce Motion | PASS | 替代模式观察=true；低动态阶段 98 次；武器 Trail 禁用 |
| 音频压力 | PASS | 峰值 15；Cooldown 抑制 724；总丢失 0 |
| 100 Actor / 268 Pickup 压力 | PASS | 最大 Actor=239、Pickup=352、VFX=90 |
| GPU | PASS | RTX 3060 Ti / D3D12；平均 2.75 ms、P99 5.00 ms |
| 目标硬件 Draw Call / 过绘 | NOT RUN | 当前 90 秒 Player 未接 ProfilerRecorder；在 G4.2-F 记录或如不可用明确标记 |
| 独立人类视听/无障碍签字 | NOT RUN | 自动证据不能替代独立人类签字 |

90 秒验收使用 3.25× Simulation Scale；`wallFrameAverage/P99` 包含验收器主动批处理和截图停顿，不作为
1% Low 证据。最终 1× 真实时长、Boss 自然流程和 1% Low 属于 G4.2-F。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：SHA-256 `67FD98E757A87F86DFD41652E52C3D3ADAAFE2AFA0852563C7E7F52C5339F7A6`
- Player Result：SHA-256 `E9B0B53029D0CA6199CDCECF439F92DA43AF8270F336082EC3DD14AD8524A30F`

## 7. 未执行项目

- 1× 真实 12 分钟完整局、两 Boss 自然阶段：`NOT RUN`，下一包 G4.2-F 执行。
- 目标最低规格物理机器、物理 4K、Draw Call/过绘专项：`NOT RUN`。
- 独立人类视听、可访问性、法律和商业权利签字：`NOT RUN`。

## 8. 已知限制和风险

- 当前 Reduce Motion 复用 Screen Shake 开关，语义和存档兼容但设置页尚无独立命名。
- 音频压力实跑发生大量冷却抑制但未触发容量抢占；容量抢占与 P0 合并由极限自动测试覆盖。
- 90 秒 GPU 数据来自当前 RTX 3060 Ti，不能外推为最低规格认证。

## 9. 未完成项

- G4.2-F 12 分钟 1× Player、0/15/30/60 秒及 3/6/9/12 分钟证据、最终 Release 与实机交互测试。
- 外部独立人类、最低规格和法律门禁。

## 10. 下一步前置条件

- 依据用户免阶段审核的持续执行指令直接进入 G4.2-F；未执行外部门禁继续保持 `NOT RUN`。

## 11. 结论

`INCOMPLETE`。

G4.2-E 工程实现、全量测试、验证、Release Build 和 90 秒自动 Player 均通过；G4.2 总里程碑仍缺少真实
12 分钟、目标硬件专项及独立人类/法律签字，不能宣称 `COMPLETE`。
