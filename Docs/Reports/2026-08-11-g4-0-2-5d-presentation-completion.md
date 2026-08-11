# Codex 结果报告

- 任务：把《剑起青岚》Demo 从平面技术切片升级为可实际战斗验收的 2.5D 成品表现，并关闭美术、动画、武器、怪物、VFX、空间层次和菜单式交互缺口
- 里程碑：G4.0 2.5D 表现成品化完整里程碑
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：实现与证据提交 `762719a4c9606b6a86599b4166f4f721f0b4b66c`；本报告所在提交见 Git 历史
- 日期：2026-08-11

## 1. 实现范围

本里程碑按 `Docs/DemoDevelopment/30_G4_0_2_5D_PRESENTATION_COMPLETION.md` 的既定顺序，完整实现以下范围：

- 倾斜正交相机、Simulation XY 到 Unity XZ 映射、平滑跟随、震屏、透明深度排序；
- 五区正式地面、竖直场景物件、地面阴影、低矮边界和高度障碍；
- 陆青野、六种普通敌人、折枝和听风的正式方向帧与集中动画推进；
- 持续可见的游风剑、正式 Projectile/Area、拖尾、命中、死亡、状态、奖励和 Boss Telegraph VFX；
- 标题、选择、Loadout、升级、奖励、暂停、结算、据点的卡片/按钮交互与正式图标 HUD；
- 真实按钮点击进入战斗、连续 60 秒 Development Player、五个时间点截图和运行时计数；
- 全量测试、内容验证、干净 Windows x64 Release Build 和独立 Release Player Smoke。

明确未改变模拟规则、数值平衡、Content Schema、存档格式和 30 Hz Tick；未引入来源不明的第三方资产、
商业最终 3D 模型、Steam SDK、商店或多人功能。

## 2. 新增和修改文件

| 文件或目录 | 变更摘要 |
|---|---|
| `Assets/Game/Presentation/PresentationSpace.cs`、`PresentationCameraRig.cs`、`PresentationCoordinator.cs` | 统一 XZ 表现空间、倾斜相机、集中同步与深度排序 |
| `Assets/Game/Presentation/DirectionalEntityPresentation.cs`、`FormalPresentationIdResolver.cs`、`EntityViews.cs` | 正式方向动画、稳定 ID 映射、武器/实体 View 与运动同步 |
| `Assets/Game/Presentation/ProceduralMapPresentation.cs`、`ProceduralPresentationProfiles.cs` | 五区正式地图层、竖直物件、阴影和高度障碍 |
| `Assets/Game/Presentation/PresentationEffects.cs`、`ViewPools.cs` | Projectile/Area、拖尾、命中/死亡/状态/奖励/Boss VFX 与池化 |
| `Assets/Game/Infrastructure/QinglanFormalVisualLoader.cs`、`QinglanProceduralPresentationFactory.cs` | 正式 Sprite 按需解析、运行时 Presentation 装配 |
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs`、`GameBootstrapper.cs`、`QinglanDemoRuntimeHost.cs` | 60 秒实际 Player 验收、证据采集、正式运行时接入 |
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs`、`QinglanDemoPresenter.cs` | 卡片/按钮交互、输入共用命令、正式 HUD 图标与状态条 |
| `Assets/Tests/EditMode/*`、`Assets/Tests/PlayMode/*` | 架构、2.5D、UI、正式视觉及实体运动同步回归 |
| `Scripts/run-qinglan-g40-visual-acceptance.ps1` | 60 秒 Player 启动、超时、JSON、日志及五图门禁 |
| `Docs/ADR/0031-g4-0-2-5d-presentation-completion.md` | 固化 G4.0 表现后端、依赖边界和回滚约束 |
| `Docs/ARCHITECTURE.md`、`Docs/MASTER_PLAN.md`、`Docs/EXECUTION_ORDER.md`、Demo 开发文档 | 对齐 G4.0 架构、范围、顺序和完成状态 |

## 3. 关键架构决定

- ADR 0031：模拟仍只产生真值与表现请求；Unity XZ 映射、相机、动画、Sprite、VFX、UI 和池全部留在 Presentation/Infrastructure/UI。
- 继续采用集中动画推进和池化 View，不给每个敌人增加 `MonoBehaviour.Update`。
- 正式 Presentation ID 解析集中在 `FormalPresentationIdResolver`，旧 Placeholder ID 只作为内容兼容输入，不作为屏幕输出资源。
- `QinglanFormalVisualLoader` 对 CatalogOnly 的 UI/VFX Sprite 按需加载并缓存，避免 HUD 统一退回同一焦点框。
- 页面鼠标、键盘和手柄均调用既有 Presenter/Command，不建立第二套游戏状态。
- 修复 `EntityView.ApplyMotion` 将渲染根节点 XZ 每帧清零的问题；新增 PlayMode 回归验证模拟位移与 View 同步。

## 4. 实际执行的命令

```text
dotnet restore Game.Tests.PlayMode.csproj
dotnet build Game.Presentation.csproj --no-restore
dotnet build Game.UI.csproj --no-restore
dotnet build Game.Infrastructure.csproj --no-restore
dotnet build Game.Tests.PlayMode.csproj --no-restore
.\Scripts\test.ps1 -Platform EditMode
.\Scripts\test.ps1 -Platform PlayMode
.\Scripts\validate.ps1 (收尾复验首次调用：FAIL，未设置 UNITY_PATH，Unity 未启动)
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\validate.ps1 (沙箱内重试：FAIL，Unity exit 198，无可用许可证)
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\validate.ps1 (本机已激活环境：PASS)
.\Scripts\build-windows-development.ps1
.\Scripts\run-qinglan-g40-visual-acceptance.ps1
.\Scripts\build-windows-release.ps1
.\Scripts\run-player-smoke.ps1 -ExecutablePath Builds/WindowsRelease/AzureSword.exe -ReleaseCandidate
git -c safe.directory=E:/ai/free-world -c core.sshCommand="ssh -p 443" push ssh://git@ssh.github.com:443/free-world-team/free-world.git HEAD:refs/heads/codex/qinglan-demo-implementation
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | Presentation 0 error/0 warning；UI 0 error/2 个既有 TMP obsolete warning；Infrastructure 0 error/15 个既有 DTO warning；PlayMode Tests 0 error |
| Focused EditMode | PASS | `TestResults/QinglanDemo/G4.0/focused-editmode.xml`：8/8，0 skipped |
| Focused PlayMode | PASS | `TestResults/QinglanDemo/G4.0/focused-playmode.xml`：1/1，0 skipped |
| 全量 EditMode | PASS | `TestResults/editmode.xml`：462/462，0 skipped；SHA-256 `ED465F6F573B4042B9FC1204EB1181B62D5FC698CAB3F35130101085DD107622` |
| 全量 PlayMode | PASS | `TestResults/playmode.xml`：22/22，0 skipped；SHA-256 `E1BD9E71354E90ADB99F962A21A5BD7B7B19F9DE67850073E941B00B9F14BD2A` |
| 内容验证 | PASS | 收尾复验后的 `TestResults/validation.log`；SHA-256 `973693439C8B9916472B1A4F08DA6CFAAD030BB16512DDA7D901D6952A27B3D5` |
| 60 秒 Player 视觉门禁 | PASS | 60.1505 秒墙钟、195.0667 秒模拟、1920×1080、真实卡片点击 1、截图 5；JSON/日志 Hash 见下文 |
| Windows Release Build | PASS | Unity 6000.3.20f1，StandaloneWindows64，非 Development，Manifest `Succeeded`、干净树、0 Placeholder、0 未批准资产 |
| Release Player Smoke | PASS | `TestResults/QinglanDemo/G3.6/release-player.json`，退出码 0，正式视觉/音频/字体/本地化和完整生命周期均通过 |
| G4.0 专用性能/Soak | NOT RUN | 60 秒验收使用 3.25× 模拟和同步截图，帧/GPU/GC 只能作为观测值，不能替代目标硬件性能基准；G4.0 DOD 未要求新的正式性能门禁 |

### G4.0 完成定义

| DOD | 结果 | 证据摘要 |
|---|---|---|
| DOD-01 倾斜正交相机与 XZ 映射 | PASS | 正交相机、47.3859° 倾角、XZ Ground Plane 均由 Player JSON 实测为真 |
| DOD-02 地面、竖直物件、阴影、高度障碍和排序 | PASS | 432 正式地块、65 正式竖直物件、65 地面阴影、13 高度几何；五图逐张审查通过 |
| DOD-03 玩家、六敌、两 Boss 动画与集中推进 | PASS | 正式目录/映射专项测试通过；60 秒局内方向帧变更 35,021 次；无逐敌人 Update |
| DOD-04 游风剑与正式 Projectile/Area | PASS | 持有武器峰值 1，Projectile 峰值 7、Area 峰值 91、拖尾 542 |
| DOD-05 正式战斗 VFX | PASS | 命中 931、死亡 519、状态 167、正式 VFX 980，峰值活动 VFX 13 |
| DOD-06 卡片/按钮与图标 HUD | PASS | 实际按钮点击 1；键鼠/手柄复用 Presenter/Command；UI 与 150%/高对比回归通过 |
| DOD-07 连续 60 秒 Player 证据 | PASS | 60.1505 秒；玩家、武器、草灵/纸鹤灵/木剑傀三类敌人、攻击、VFX、掉落均在实际 Player 中出现 |
| DOD-08 EditMode、PlayMode、Validation | PASS | 462/462、22/22、Validation PASS |
| DOD-09 Windows Release Build 与 Smoke | PASS | `AzureSword.exe` 和 Build Manifest PASS；独立 Release Player Smoke PASS |
| DOD-10 画面审查 | PASS | 五张实际 Player 截图逐张视觉审查：角色/怪物/武器/VFX/立体场景可辨识，不再是随机平面拼图或纯文字菜单 |

### 60 秒运行时证据

- JSON：`TestResults/QinglanDemo/G4.0/player-60s.json`，SHA-256 `1B4EC753BC18B3895484AF1BDAE09704DCF78F637344555D89855071DAEE2F4C`。
- 日志：`TestResults/QinglanDemo/G4.0/player-60s.log`，SHA-256 `A56A76B931AA452FB43767538B8FDA5DAD55815A2BF0A0ECEBCFFFF31E0BB506`。
- 累计观测 View 11,916；活动 View 峰值 279，其中 Actor 106、Projectile 7、Area 91、Pickup 114。
- 正式 VFX 980；实际创建的池对象 82；动画帧变更 35,021；缺失 Profile 程序化降级 0。
- 世界实体不存在仍使用程序化降级的稳定 ID。缺少专属被动/心诀图标的 HUD 槽位仍使用正式通用 `ui.focus` Sprite；这不是程序化图形，但仍是内容特异性不足的已知美术限制。
- 五张截图 SHA-256：`00-enter-combat`=`45D0BB26...13E62`、`15-seconds`=`7A0C8C71...2E4F0`、
  `30-seconds`=`6E7A968B...0F2B`、`45-seconds`=`81315FC0...59043`、`60-seconds`=`99BBB570...AE0A`。

### GPU、内存与池观测

- 60 秒局墙钟帧平均 179.9215 ms、p99 265.8928 ms；这是 3.25× 模拟和截图 I/O 下的验收驱动时序，不是游戏帧率结论。
- GPU 样本 222，平均 3.7038 ms、p99 113.4075 ms；p99 包含同步截图尖峰，不作为目标 GPU 回归判定。
- Mono Used 从 16,465,920 B 到 21,204,992 B，峰值 22,339,584 B；Total Allocated 从 231,985,664 B 到 238,097,260 B，峰值 238,064,707 B。
- Gen0/Gen1/Gen2 均为 8 次；采集过程含截图、按需 Addressables/UI 和 JSON 输出，不能外推为高频模拟路径分配。
- View 与 VFX 均继续池化；峰值活动 View 279、VFX 13，创建 VFX 对象 82。实体位移修复未引入逐实体 Update。
- 结论：未观察到池失控或运行时缺图回退；正式 G4 后目标硬件 GPU/1% Low 回归 `NOT RUN`，发布前仍应单独重跑。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development / NullPlatformFacade / Offline Required
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：`Builds/WindowsRelease/BuildManifest.json`，SHA-256 `25AF7B84AB2F32BDDF7FCC3D3B8554EC91CF657086184E5011DAF36384C6AD63`
- Manifest Git：`762719a4c9606b6a86599b4166f4f721f0b4b66c`，`workingTreeClean=true`
- Release 内容：`qinglan.pack.demo` 0.10.0，193 definitions，Placeholder=0，未批准资产=0，Release Validator=`PASS`

## 7. 未执行项目

- G4.0 专用目标硬件 GPU/1% Low/长时性能回归：`NOT RUN`。60 秒视觉驱动不具备有效的性能采样条件，
  且不属于 G4.0 DOD；发布前应在正式表现代码上重跑专用目标硬件基准。
- 独立人类最终视听签字、商业权利/Steam AI 披露签字、最低规格物理机器认证：`NOT RUN`。
  这些是既有 G3.6 Release 外部门禁，不属于 G4.0 表现修复的完成定义。
- GitHub 自托管 CI：本里程碑未重新执行；本机全量门禁和干净 Release 构建已执行。
- 收尾复验第一次未设置 `UNITY_PATH`，第二次在沙箱身份下缺 Unity entitlement（exit 198）；两次都没有形成有效测试结果。
  随后在本机已激活环境以相同 Unity 版本重新执行并 `PASS`，最终验证日志和上表 Hash 均来自成功复验。

## 8. 已知限制和风险

- G4.0 完成不等于商业 Release `GO`；QD-KI-003、QD-KI-014 与最低规格物理机器证据仍阻止发布。
- 角色与敌人使用已批准的正式 2D 方向 Sprite 和 2.5D 场景合成，不是商业最终 3D 模型。
- 部分被动/心诀 HUD 缺专属图标时使用正式通用 `ui.focus` Sprite，后续美术批次可补齐，但不影响 G4.0 世界实体验收。
- 60 秒视觉验收为了在固定墙钟内覆盖三种敌人使用 3.25× 模拟；战斗逻辑仍运行在既有固定 30 Hz Tick。

## 9. 未完成项

- G4.0 当前强制项：无。
- 全产品发布项：独立人类/法律签字、最低规格认证、正式表现代码的目标硬件性能复测仍未完成。

## 10. 下一步前置条件

- 若进入 Release/Steam/商店工作，必须先关闭 QD-KI-003、QD-KI-014，取得最低规格物理机器 PASS，
  并在当前 G4.0 正式表现代码上重跑目标硬件 GPU/1% Low 基准。
- 未经用户新授权，不合并 `main`、不打 Release 标签、不创建商店提交。

## 11. 结论

`COMPLETE`

G4.0 DOD-01—DOD-10 均有实际 `PASS` 证据，代码提交、每阶段 Push、60 秒 Player、全量门禁、
Windows Release Build 与独立 Player Smoke 已完成。该结论只关闭 G4.0，不覆盖仍为 `NOT RUN` 的全产品外部 Release 门禁。
