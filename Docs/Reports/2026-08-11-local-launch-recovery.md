# Codex 结果报告

- 任务：诊断并修复本机双击 Demo 无响应
- 里程碑：G3.6 Release Candidate 本地人工体验入口修复
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`7e965bd`
- 日期：2026-08-11

## 1. 实现范围

- 检查用户双击后的实际进程、窗口句柄、Player 日志和 Windows Application 事件。
- 确认用户启动的是历史 `Builds/WindowsRelease/AzureSword.exe`：进程驻留但无窗口，Windows 记录 `AppHangB1`。
- 新增仓库根目录可双击启动器，优先启动已包含画面修复的本机视觉探针，并显式采用 1600×900 窗口模式。
- 将未来 Windows Player 的默认启动配置改为 1600×900、非原生分辨率、可调整大小的窗口模式。
- 实际双击等价启动并连续采样 30 秒，确认窗口存在且持续响应；捕获实际首屏截图。
- 未重新生成正式 Release Build；本机 Unity Editor entitlement 问题仍未解除。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Run-Qinglan-Demo.cmd` | 新增可双击入口；优先选择已修复探针，回退到 Release Player，显式使用 1600×900 窗口模式并输出启动日志。 |
| `ProjectSettings/ProjectSettings.asset` | 将未来构建默认改为 1600×900、Windowed、非原生分辨率、窗口可调整大小。 |
| `README.md` | 说明人工体验必须使用仓库根目录启动器，避免误开历史构建。 |
| `Docs/Reports/2026-08-11-local-launch-recovery.md` | 本报告。 |

## 3. 关键架构决定

- 本机立即可用入口与未来构建默认设置同时修复：启动器解决现有构建，PlayerSettings 解决后续全新构建。
- 启动器不修改模拟、内容、存档或运行时依赖，只负责选择 Player、传入显示参数和保存日志。
- 保持 Direct3D 12；诊断证明窗口化 D3D12 能稳定运行，因此没有无证据切换渲染后端。
- 不覆盖或伪装历史 Release 产物；启动器明确优先使用已验证的隔离视觉探针。
- 未改变长期架构边界、Schema、Tick 或第三方依赖，无需新增 ADR。

## 4. 实际执行的命令

```text
Get-Process -Name AzureSword ...
Get-WinEvent -FilterHashtable @{LogName='Application'; ...}
Start-Process Builds/WindowsVisualFixProbe-c780b39/AzureSword.exe -ArgumentList -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile ...
Start-Process Run-Qinglan-Demo.cmd
Get-Process -Name AzureSword ...（15 次、每 2 秒窗口稳定性采样）
System.Drawing.Graphics.CopyFromScreen(...)（捕获实际 Player 窗口）
git -c safe.directory=E:/ai/free-world commit -m "fix make local demo launch reliably"
git -c safe.directory=E:/ai/free-world -c "core.sshCommand=ssh -o StrictHostKeyChecking=yes -p 443" push ssh://git@ssh.github.com:443/free-world-team/free-world.git HEAD:refs/heads/codex/qinglan-demo-implementation
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 原问题复现 | PASS | 历史 Release 进程 `MainWindowHandle=0`、`Player.log=0 B`；Windows Application 事件为 `AppHangB1`。 |
| 新启动器实际启动 | PASS | 启动路径为 `WindowsVisualFixProbe-c780b39/AzureSword.exe`，窗口标题 `AzureSword`，窗口句柄有效，进程响应正常。 |
| 30 秒窗口稳定性 | PASS | 15/15 个两秒采样均 `HasExited=False`、`Responding=True`、窗口句柄 `23856560`。 |
| 运行时初始化 | PASS | 日志包含 `Map tiles=80, props=80` 和 `packs=1, entries=193`。 |
| 实际首屏画面 | PASS | `TestResults/QinglanDemo/ManualLaunch/window.png`：1616×939 窗口捕获，背景、角色、标题与菜单可见。 |
| PlayerSettings 静态检查 | PASS | 1600×900、`defaultIsNativeResolution=0`、`resizableWindow=1`、`fullscreenMode=3` 全部匹配。 |
| EditMode | NOT RUN | 本次未改变纯逻辑；本机 Unity Editor 无有效 entitlement。 |
| PlayMode | NOT RUN | 本机 Unity Editor 无有效 entitlement。 |
| 新 Release Build | NOT RUN | 不能在当前 Unity 许可证状态下生成全新正式构建。 |
| 性能/Soak | NOT RUN | 本次为本地启动入口修复，不修改模拟或性能热点。 |

## 6. 构建产物

- 配置：既有非 Debug Player 外壳 + 已验证托管程序集的本机视觉探针；不是本次新构建的正式 Release。
- 路径：`Builds/WindowsVisualFixProbe-c780b39/AzureSword.exe`
- EXE SHA-256：`34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- 启动器 SHA-256：`245DAAC9358719C8A3866AAF2E8A55EB707876538D805D0648F6CD0F50B25C5C`
- 首屏截图 SHA-256：`778C75ACB29B3FBDE8C7B4F802EE3F6A731D01C82CDD6E96292014349FD7ED4F`
- Build Manifest：NOT RUN；本次没有生成新的 Build Manifest。

## 7. 未执行项目

- 没有从 `7e965bd` 生成全新 Windows Release，因为本机 Unity 6 `6000.3.20f1` 的 Editor entitlement 仍不可用。
- 没有执行 Unity EditMode/PlayMode；不得把双击实机验证描述为 Unity Test Framework 通过。
- GitHub CLI 已安装，但当前 `justice-zhang` token 无效；本次仅按用户要求直接提交并通过 SSH 推送分支，没有创建 PR。

## 8. 已知限制和风险

- 当前启动器优先使用本机存在且已验证的 `WindowsVisualFixProbe-c780b39`；该目录被 `.gitignore` 排除，不是仓库克隆后自动具备的产物。
- 其他机器需要先从当前分支生成新 Player，启动器才会回退到 `Builds/WindowsRelease/AzureSword.exe`。
- PlayerSettings 默认窗口配置只有在下次正式构建后才进入 EXE；当前即时修复依赖启动器命令行参数。

## 9. 未完成项

- 恢复 Unity Editor entitlement 后生成当前提交的全新 Windows Release。
- 对全新 Release 直接双击进行同样的窗口稳定性、PlayMode 和 Release Player 门禁验证。

## 10. 下一步前置条件

- 使用仓库根目录 `Run-Qinglan-Demo.cmd` 进行当前本机人工体验。
- 正式候选构建需要有效 Unity 6 `6000.3.20f1` 许可证，或恢复已激活的自托管 Unity Runner。

## 11. 结论

`INCOMPLETE`

当前本机双击启动入口已实际验证为可用，窗口和首屏画面稳定显示；但全新正式 Release Build 未执行，不能声明正式候选构建门禁完成。
