# Codex 结果报告

- 任务：按 32 号生产规范完成 G4.2-C 环境、光照与五区重制
- 里程碑：G4.2-C
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：`1974952ce90f441f8347320de783a66081be638e`
- 日期：2026-08-24

## 1. 实现范围

在既有倾斜正交 XZ 表现架构内完成五区世界重排：加入 32 个不规则软边界元素、5 个低频区域身份簇、
区域化道具色调和小/中/大轮廓层级；把 3 个目标与 5 个地标接入现有 Addressables 三态正式图集；
增加接地光环、阴影和垂直展示，并让镜头依据玩家移动方向产生受地图边界约束的 1.2 米前瞻。

本包只修改 Presentation/Infrastructure 投影，不改变 Walkable、碰撞、生成、伤害或 Simulation Tick。
没有导入第三方资产、包、Shader 或 Renderer Feature。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Presentation/ProceduralMapPresentation.cs` | 五区软边界、身份簇、区域化道具、正式三态地图标记与指标 |
| `Assets/Game/Presentation/ProceduralMapMarkerSpriteSet.cs` | 不可变的地图标记状态精灵集合 |
| `Assets/Game/Presentation/PresentationCameraRig.cs` | 受边界约束的动态镜头前瞻 |
| `Assets/Game/Presentation/PresentationCoordinator.cs` | 暴露五区和正式标记验收指标 |
| `Assets/Game/Infrastructure/QinglanFormalVisualLoader.cs` | 通过 Addressables 加载 8 组三态目标/地标图集 |
| `Assets/Game/Infrastructure/QinglanProceduralMapFactory.cs` | 以稳定 `ContentId` 注入正式状态图 |
| `Assets/Game/Infrastructure/QinglanDemoRuntimeHost.cs` | 将正式状态图 Catalog 接入地图构建 |
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs` | Player 采集并硬门禁 G4.2-C 指标 |
| `Assets/Tests/EditMode/QinglanG42VisualThemeTests.cs` | 软边界、五区簇、三态切换和接地测试 |
| `Assets/Tests/EditMode/M7PresentationUiInputTests.cs` | 镜头前瞻方向、距离测试 |
| `Assets/Tests/PlayMode/QinglanG26UiInputPlayModeTests.cs` | 真实 Bootstrap/Addressables 状态图加载测试 |
| `Scripts/run-qinglan-g42-visual-acceptance.ps1` | 外层 Player 五区指标门禁 |

## 3. 关键架构决定

- 保留现有地图配置、稳定 ID 和集中 Presentation，同一状态精灵集合只在启动时通过 Addressables 加载。
- 装饰边界与区域身份全部为表现对象，不加入 Simulation 或 Walkable 数据。
- 没有改变 Assembly、Schema、存档、Tick、渲染后端或第三方依赖，未新增 ADR。

## 4. 实际执行的命令

```text
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.QinglanG42VisualThemeTests
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.M7PresentationUiInputTests.CameraBoundsAndEffectsToggleAreHonored
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform PlayMode -testFilter Game.Tests.PlayMode.QinglanG26UiInputPlayModeTests.ActiveRunHudRevealsBattlefieldAndMapUsesOnlyAForegroundOverlay
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform PlayMode
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.ProjectValidationCommand.Run
Scripts/build-windows-release.ps1 -OutputPath Builds/WindowsRelease/AzureSword.exe
Scripts/run-qinglan-g42-visual-acceptance.ps1 -Executable Builds/WindowsRelease/AzureSword.exe -ScreenWidth 1920 -ScreenHeight 1080
git commit -m "feat(g4.2-c): build stateful five-region world"
git push origin codex/qinglan-demo-implementation
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译/聚焦 EditMode | PASS | G4.2 主题与世界 8/8；镜头 1/1 |
| 全量 EditMode | PASS | `471/471`，`TestResults/QinglanDemo/G4.2-C/Full/editmode.xml` |
| 全量 PlayMode | PASS | `24/24`，`TestResults/QinglanDemo/G4.2-C/Full/playmode.xml` |
| 内容验证 | PASS | `[Project Validation] PASS` |
| 正式地图资源 | PASS | 80 地块、80 道具、8 组/24 张状态精灵均由 Addressables 加载 |
| Release Build | PASS | `WindowsReleaseCandidate`，Manifest `Succeeded`、`workingTreeClean=true` |
| 90 秒 Player 自动门禁 | PASS | 92.91 秒，15 张截图，窗口持续响应，自动 Gate=`true` |
| GPU 初测 | PASS | RTX 3060 Ti / D3D12；326 样本，平均 2.70 ms，P99 5.38 ms |
| 五区独立人类盲测 | NOT RUN | 用户要求全程不等待人工审核；自动结构证据不能冒充独立人类判断 |

Player 同时记录：32 个过渡元素、5 个区域身份簇、8 个正式地图标记、24 张状态图、最大 239 Actor、
352 Pickup、90 Active VFX、正式表现降级次数 0。验收运行使用 3.25× Simulation Scale，GPU 样本可作为
当前机器初测；其中 Wall Frame 包含截图与验收器开销，不作为最终 1% Low 结论。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：SHA-256 `0F04068D47DE4F0D48F1B1101534387A874E925E7A866CA6C331951C4F2BF87C`

## 7. 未执行项目

- 独立人类五区盲测、地面接缝主观审查和艺术方向签字：`NOT RUN`。
- 最低规格物理机器与 4K 实机：`NOT RUN`；留到 G4.2-F，不能由当前 RTX 3060 Ti 结果外推。
- 真实时长 12 分钟非加速局：`NOT RUN`；属于 G4.2-F。

## 8. 已知限制和风险

- 现有五区正式底图资源仍共享同一总体像素语言，结构差异已增强，但独立人类是否能盲辨至少四区仍未知。
- 正式地图标记完成态由自动单元测试覆盖；本次 90 秒 Player 中观察到活动态，未自然推进到完成态。
- 物理 4K、最低规格 GPU、过绘和 Draw Call 尚无目标硬件证据。

## 9. 未完成项

- G4.2-D 角色、六敌、两 Boss 与游风剑重制。
- G4.2-E VFX、反馈与音频混音。
- G4.2-F 12 分钟实机、目标硬件、法律和独立人类最终验收。

## 10. 下一步前置条件

- 按用户持续执行授权进入 G4.2-D；所有独立人类门禁继续保留 `NOT RUN`，不伪报通过。

## 11. 结论

`INCOMPLETE`。

G4.2-C 工程实现、自动测试、内容验证、Release Build、90 秒 Player 与当前机器 GPU 初测均已完成；
但强制的独立人类五区盲测未执行，因此里程碑不能写成 `COMPLETE`。依据用户明确的免阶段审核指令继续 G4.2-D。
