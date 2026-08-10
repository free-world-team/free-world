# Codex 结果报告

- 任务：完成《剑起青岚》Demo G3.4 数值冻结、全量门禁与证据归档
- 里程碑：G3.4
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`6590526`、`8c9fc77`、`d500d79`、`b0c0295`、`5c89abe`
- 日期：2026-08-10

## 1. 实现范围

实现无作弊命令驱动自动玩家、15 局三构筑矩阵、三 Golden 重放、三失败探针、18 个奇物兼容用例、冻结
内容数值和机器可读报告。最终候选同步了 Pack `0.10.0`、内容烘焙、旧里程碑 Golden 与完整门禁。
未提前实现 G3.5 性能优化或 G3.6 Release Candidate。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Editor/QinglanG34BalanceCommand.cs` | 命令级矩阵、重放、失败探针、兼容和报告 |
| `Assets/Game/Editor/QinglanG34BalanceContentSetup.cs` | 冻结数值与 Pack `0.10.0` |
| `Assets/Game/Editor/QinglanG22ContentSetup.cs` | 同步最终 Boss 800 生命 canonical setup |
| `Assets/GameAssets/Placeholder/QinglanDemo/*` | 冻结 authoring 与 baked catalog |
| `Assets/Tests/EditMode/QinglanG34BalanceContractTests.cs` | 自动玩家与判定合同 |
| `Assets/Tests/EditMode/QinglanG12/G13/G14/G23/G24*Tests.cs` | 更新受控 Golden 与 Pack 断言 |
| `Docs/DemoDevelopment/Assets/G3.4/*` | 冻结清单、完整矩阵和三条 Golden |

## 3. 关键架构决定

- 自动玩家只通过 `RunSession` 提交玩家命令；World 访问仅用于只读遥测。
- 最终 Boss 生命冻结为 800：50 会跳过中间阶段，2600 会让多条路线超时，800 同时满足平衡和三阶段可读性。
- 没有改变 Tick、Schema、程序集方向、存档格式或第三方包，因此无需新增 ADR。

## 4. 实际执行的命令

```text
Unity.exe -batchmode -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG34BalanceContentSetup.Configure
Unity.exe -batchmode -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG34BalanceCommand.Run
Unity.exe -batchmode -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testResults ...\editmode-pass.xml
Unity.exe -batchmode -projectPath E:\ai\free-world -runTests -testPlatform PlayMode -testResults ...\playmode.xml
Unity.exe -batchmode -projectPath E:\ai\free-world -executeMethod Game.Editor.ProjectValidationCommand.Run
Unity.exe -batchmode -projectPath E:\ai\free-world -executeMethod Game.Editor.WindowsDevelopmentBuild.BuildFromCommandLine
AzureSword.exe -batchmode -nographics -qinglanG28Smoke
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | Development Build 日志 `[M0 Build] PASS` |
| EditMode | PASS | 451/451，`editmode-pass.xml` |
| PlayMode | PASS | 20/20，`playmode.xml` |
| 内容验证 | PASS | `project-validation.log` `[Project Validation] PASS` |
| 构建 | PASS | Windows x64 Development Build |
| 性能/Soak | PASS | 15 局矩阵 12/15、三 Golden、三失败探针、18/18 奇物兼容 |
| Player Smoke | PASS | 正式视觉、音频、字体、本地化、流程、Save 均通过 |

## 6. 构建产物

- 配置：Windows x64 Development
- 路径：`Builds/WindowsDevelopment/AzureSword.exe`
- 文件 Hash：`5d7eeb5359c2e35e4eb1f6a5844b25c3d7556795bd2f15ec234a2011406bc9c6`
- Build Manifest：`Builds/WindowsDevelopment/BuildManifest.json`
- Manifest Hash：`8ded78cec57912dc456127576ded728583fc8fe276639e5d1f9c5f7488e535e2`

## 7. 未执行项目

- G3.5 目标硬件 1080p60/1% Low 性能门禁：不属于 G3.4，按单里程碑纪律未执行。
- G3.6 Release Build 与平台后端验收：不属于 G3.4，未执行。

## 8. 已知限制和风险

- 矩阵是确定性命令自动玩家证据，不能替代 G3.5 的真实目标硬件 GPU/CPU 捕获和人工手感评审。
- Development Player 冒烟不等于 Release Player 商店分发验证。

## 9. 未完成项

- 当前里程碑无未完成项。

## 10. 下一步前置条件

- 在干净的 `codex/qinglan-demo-implementation` 分支进入 G3.5，一次只处理性能基线与优化门禁。

## 11. 结论

`COMPLETE`
