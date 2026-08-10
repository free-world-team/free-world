# Codex 结果报告

- 任务：完成《剑起青岚》Demo G3.5 目标硬件性能取证、全量门禁与证据归档
- 里程碑：G3.5
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`6e143ef`、`6db1e29`、`3955607`、`2ad6d11`、`bec1c43`
- 日期：2026-08-10

## 1. 实现范围

实现 opt-in Development Player 性能 Runner、正式 Sprite 压力探针、FrameTiming/ProfilerRecorder 采样、
预分配报告合同、PowerShell 驱动与聚焦测试；完成正式内容 54,000 Tick CPU 基线、1080p 30 分钟 GPU
Target、2,000 Enemy 扩展观察、全量回归、Project Validation、Development Build 与 Player Smoke。
未提前生成 G3.6 Release Candidate、干净克隆发布包或商店签署材料。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Infrastructure/QinglanG35PerformanceRunner.cs` | 真实 Player 预热、30 Hz/60 FPS 驱动、GPU/GC/内存/渲染采样与退出判定 |
| `Assets/Game/Infrastructure/QinglanG35FormalPresentationProbe.cs` | 生产 RenderSnapshot 到正式 Sprite 的预热压力表现 |
| `Assets/Game/Infrastructure/QinglanG35PerformanceReport.cs` | JSON DTO、分位/1% Low、内存趋势与预算合同 |
| `Assets/Tests/EditMode/QinglanG35PerformanceContractTests.cs` | 分位、完整 PASS、缺 GPU、焦点/远程适配器和内存增长测试 |
| `Scripts/run-qinglan-g35-performance.ps1` | CPU、Quick、Target、Extension 四种可复现入口及结果复核 |
| `ProjectSettings/ProjectSettings.asset` | 启用 Player Frame Timing Stats |
| `Docs/DemoDevelopment/Assets/G3.5/*` | CPU/GPU Target、扩展压力、Build/Smoke 与最终汇总证据 |

## 3. 关键架构决定

- 模拟仍由无 UnityEngine 依赖的 M10 生产 Store/系统驱动；Infrastructure 只通过 friend assembly 复用内部压力夹具，
  没有反向程序集引用。
- 正式表现探针在预热期一次性创建固定 View/SpriteRenderer，测量窗只消费 `RenderSnapshot`，不改变模拟真值。
- 百分位在结束后原地排序；测量窗不使用 LINQ、字符串格式化、临时集合或逐帧日志。
- 正式 1,200 Enemy Target 全部通过，因此不实施无证据优化；2,000 Enemy 扩展 `FAIL` 保留为非阻断 CPU
  容量拐点，不修改 G3.4 冻结玩法。
- 没有改变 Tick、Content Schema、存档格式、程序集依赖方向或第三方包，因此无需新增 ADR。

## 4. 实际执行的命令

```text
Unity.exe -batchmode -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.QinglanG35PerformanceContractTests ...
Game.Editor.WindowsDevelopmentBuild.BuildFromCommandLine
Scripts/run-qinglan-g35-performance.ps1 -Mode Quick ...
Scripts/run-qinglan-g35-performance.ps1 -Mode Cpu ...
Scripts/run-qinglan-g35-performance.ps1 -Mode Target ...
Scripts/run-qinglan-g35-performance.ps1 -Mode Extension ...
Unity.exe -batchmode -projectPath E:\ai\free-world -runTests -testPlatform EditMode ...
Unity.exe -batchmode -projectPath E:\ai\free-world -runTests -testPlatform PlayMode ...
Unity.exe -batchmode -projectPath E:\ai\free-world -executeMethod Game.Editor.ProjectValidationCommand.Run ...
Unity.exe -batchmode -projectPath E:\ai\free-world -executeMethod Game.Editor.WindowsDevelopmentBuild.BuildFromCommandLine ...
Scripts/run-qinglan-g28-player-smoke.ps1 ...
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | 全量测试与 Development Build 无 C# 编译错误/告警 |
| G3.5 聚焦 EditMode | PASS | 5/5 |
| 全量 EditMode | PASS | 456/456，0 失败、0 跳过 |
| 全量 PlayMode | PASS | 20/20，0 失败、0 跳过 |
| 内容验证 | PASS | `[Project Validation] PASS` |
| Development Build | PASS | `[M0 Build] PASS`；Manifest 为当前干净提交 `bec1c43` |
| Player Smoke | PASS | 正式视觉/音频/字体/本地化、UI、单局、Save 与重开均通过 |
| CPU Target | PASS | 54,000 Tick；p99 18.036 ms；9,501 实体；0 B/GC 0 |
| GPU Target | PASS | 107,986 Frame；59.992 FPS；1% Low 59.891；GPU p99 2.163 ms；Tick p99 12.760 ms |
| 2,000 Enemy 扩展 | FAIL | 非发布阻断观察；43.694 FPS、1% Low 36.768；GPU p99 2.822 ms、Tick p99 23.685 ms |

## 6. 构建产物

- 配置：Windows x64 Development
- 路径：`Builds/WindowsDevelopment/AzureSword.exe`
- 文件 Hash：`5d7eeb5359c2e35e4eb1f6a5844b25c3d7556795bd2f15ec234a2011406bc9c6`
- Build Manifest：`Builds/WindowsDevelopment/BuildManifest.json`
- Manifest Hash：`b6852e6e23017524b79d868c2b11c1cabb7034ba5056c9b836ba9f3ed208a661`

## 7. 未执行项目

- 初始 4 核、8 GB、DX11、2 GB VRAM 最低规格实机认证：当前仅有 i7-12700F / RTX 3060 Ti 参考机，未伪造
  最低规格结论。
- G3.6 Release Build、干净克隆、Release Manifest、分发包和法律/商店合规：不属于 G3.5，未执行。

## 8. 已知限制和风险

- 2,000 Enemy 扩展显示 CPU 容量拐点：平均 43.694 FPS、1% Low 36.768；若未来把发布目标提升到 2,000
  同屏敌人，需单独里程碑评估 EnemyDecision 和表现同步，不得把本轮正式 Target PASS 外推。
- 参考机结果不能替代最低规格与更多 GPU/驱动矩阵认证。
- Development Player 不是 G3.6 Release Player。

## 9. 未完成项

- 当前里程碑强制项无未完成项；扩展压力已按设计实际运行并明确记录 `FAIL`，不构成 G3.5 发布阻断。

## 10. 下一步前置条件

- 在干净的 `codex/qinglan-demo-implementation` 分支进入 G3.6，只处理 Release Candidate、干净克隆、
  Manifest、Smoke、合规包和 DOD-01—10。

## 11. 结论

`COMPLETE`
