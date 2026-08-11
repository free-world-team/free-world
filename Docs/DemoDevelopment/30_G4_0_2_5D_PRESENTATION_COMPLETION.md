# G4.0：2.5D 表现成品化完整里程碑

## 1. 目标

把当前“静态 2D 技术切片”升级为可以按实际战斗画面验收的《剑起青岚》Demo。该里程碑必须一次性关闭
用户指出的全部表现缺口：美术运行时使用、战斗特效、武器、怪物、空间层次、2.5D 感觉和菜单式交互。

## 2. 范围

### G4.0-A 2.5D 战场

- 倾斜正交相机、玩家略低于屏幕中心、平滑跟随与震屏；
- Simulation XY 到 Unity XZ 的统一表现映射；
- 正式地块贴地、竖直场景物件、地面阴影、高度障碍、区域层和透明深度排序；
- 五区在色调、装饰采样和地标上可辨识，不能再表现为随机整图拼贴。

### G4.0-B 角色、怪物与 Boss

- 陆青野 Idle/Move 方向帧；
- 六种普通敌人 Idle/Move 方向帧；
- 折枝、听风 Idle/Move 与阶段脉冲；
- 受击、死亡、精英词缀、Boss 阶段至少有明确视觉覆盖；
- View 继续池化、集中动画，不增加逐敌人 Update。

### G4.0-C 武器与战斗 VFX

- 游风剑作为玩家装备在战斗中持续可见；
- Projectile/Area 使用正式技能 Sprite，按运动方向旋转或铺设；
- 形成放出、飞行/铺场、命中、死亡、Boss Telegraph、状态和奖励反馈；
- 正式 VFX Sprite 优先，程序化图形只作为缺失降级；
- 拖尾、地面投影、闪光与音频必须受现有可访问性/混音设置控制。

### G4.0-D 成品 UI

- 标题、角色、地图、Loadout、升级、奖励、暂停、结算和据点使用可点击卡片/按钮；
- 鼠标、键盘和手柄共用 Presenter/Command，不建立第二套状态；
- HUD 包含生命/护盾/经验条、武器/心诀图标、Boss 条、时间/风势和目标摘要；
- 150% 字号、高对比、焦点形状和本地化继续通过。

### G4.0-E 实际验收

- 连续至少 60 秒 Player 战斗；
- 截图时间点至少为进入战斗、15 秒、30 秒、45 秒和 60 秒；
- 60 秒内必须实际出现玩家、游风剑、至少三类普通敌人、攻击实体、命中/死亡 VFX、掉落或升级反馈；
- 报告必须记录峰值/累计 View、动画帧变更、正式 VFX、武器 View、按钮点击和 2.5D 层计数；
- 人工截图审查失败即里程碑 `FAIL`，不得以自动字段覆盖。

## 3. 明确非范围

- 不新增来源不明的第三方资产；
- 不制作新的商业最终 3D 模型；
- 不改变模拟规则、数值平衡、Content Schema、存档或 30 Hz Tick；
- 不加入多人、Steam SDK、商店或完整剧情演出。

## 4. 实施顺序

1. ADR/架构/门禁冻结；
2. PresentationSpace、相机和地图层；
3. 方向动画和深度排序；
4. 装备、武器、Projectile/Area、正式 VFX；
5. 按钮/卡片/HUD；
6. 60 秒 Player 视觉验收；
7. 全量测试、构建和结果报告。

前一步未通过相关编译/专项检查，不进入下一步。每一步独立 Commit 与 Push。

## 5. 完成定义

| DOD | 条件 | 结果要求 |
|---|---|---|
| DOD-01 | 倾斜正交相机与 XZ 战场映射 | PASS |
| DOD-02 | 地面、竖直物件、阴影、高度障碍和深度排序 | PASS |
| DOD-03 | 玩家、六敌人、两 Boss 动画目录与集中帧推进 | PASS |
| DOD-04 | 游风剑装备 View 和正式 Projectile/Area | PASS |
| DOD-05 | 命中、死亡、Boss、状态、奖励正式 VFX | PASS |
| DOD-06 | 页面按钮/卡片和图标 HUD，鼠标/键盘/手柄同命令 | PASS |
| DOD-07 | 60 秒 Player 证据满足全部实体/反馈条件 | PASS |
| DOD-08 | 相关 EditMode、PlayMode、Validation | PASS |
| DOD-09 | Windows Release Build 与 Player Smoke | PASS |
| DOD-10 | 人工画面审查确认不再是平面拼图/纯文字菜单 | PASS |

任一项 `FAIL` 或 `NOT RUN` 时，G4.0 结论必须为 `INCOMPLETE`。

## 6. 测试命令

```powershell
dotnet build Game.Presentation.csproj --no-restore
dotnet build Game.UI.csproj --no-restore
dotnet build Game.Infrastructure.csproj --no-restore
dotnet build Game.Tests.PlayMode.csproj --no-restore
.\Scripts\test.ps1 -Platform EditMode
.\Scripts\test.ps1 -Platform PlayMode
.\Scripts\validate.ps1
.\Scripts\build-windows-release.ps1
.\Scripts\run-player-smoke.ps1
```

Unity 许可证或 Runner 不可用时对应项只能记录 `NOT RUN`，不能降级完成定义。

## 7. 完成报告

使用 `Templates/CODEX_RESULT_REPORT.md`，额外附：

- 五张以上实际 Player 战斗截图；
- 60 秒 JSON 与日志 Hash；
- 正式动画/武器/VFX/UI 使用计数；
- 仍为程序化降级的具体稳定 ID；
- GPU/内存/池变化与性能回归结论。
