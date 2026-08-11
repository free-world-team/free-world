# ADR 0031：G4.0 倾斜正交 2.5D 表现后端与成品化验收

- 状态：Accepted
- 日期：2026-08-11
- 决策人：依据用户要求将完整 2.5D 表现里程碑全部纳入
- 关联里程碑：G4.0
- 承接：ADR 0025、0028、0029、0030

## 背景

G3.6 之前的自动门禁证明了模拟、内容、正式资产治理、Addressables、音频、字体和构建链路，但运行时
表现仍是正面正交相机、XY 平面 SpriteRenderer、静态单帧 Profile、程序化几何 VFX 和整页 TMP 文本菜单。
正式 PNG/音频“存在并可加载”被错误地等同于“成品表现已接入”。人工检查确认武器、怪物、动画、战斗
特效、可点击 UI 和 2.5D 空间感没有形成可验收闭环。

用户明确要求把这些缺失项全部纳入一个完整里程碑。该要求高于旧文档中仅验证程序化 Placeholder/静态
Sprite 的完成表述，并触发表现后端替换 ADR。

## 决策

### 1. 坐标与相机

- Simulation 的二维 `(x, y)` 保持不变；Presentation 统一映射为 Unity 世界 `(x, height, z=y)`。
- 使用倾斜正交相机：保持 Demo 方案“正交俯视”要求，同时通过俯角、纵向偏移、景深分层与可见地平面
  形成 2.5D，而不是改成透视玩法相机。
- Camera Rig 仍是唯一跟随、震屏和构图 Owner；UI 不控制 Camera，Simulation 不引用 Camera。

### 2. 地图与实体

- 地块平铺到 XZ 地面；障碍形成有高度的 3D 体块；正式场景物件为竖直 Billboard，并附加地面阴影。
- Actor、Projectile、Area、Pickup 的 View 继续池化；Actor/Prop 竖直朝向相机，Projectile/Area 可贴地或
  保持竖直，所有透明 Sprite 使用统一深度排序规则。
- 不把正式实体改成逐敌人 `Update`；动画、朝向、装备和特效由现有 Coordinator/Pool 集中推进。

### 3. 动画、装备与特效

- 正式方向动画图集由 Infrastructure 的 Addressables Owner 加载，向 Presentation 注入只读帧目录。
- Player、六类普通敌人和两个 Boss 至少具备 Idle/Move 的帧推进；攻击、受击、死亡通过事件脉冲覆盖。
- 玩家装备显示独立于 Simulation 真值：由 `RunUiSnapshot` 的已拥有技能稳定 ID 驱动只读装备 View，
  不创建第二套战斗状态。游风剑必须在玩家身边可见；投射物必须具有方向、拖尾和命中特效。
- 命中、死亡、Boss 阶段、机制、技能与状态 VFX 优先解析正式 Sprite；无法解析时才使用程序化降级。

### 4. UI

- 保留单 Canvas、单 Presenter、单输入命令入口和 UI-safe Snapshot。
- 页面选项改为池化 `Button + Image + TMP_Text` 卡片；鼠标点击、键盘和手柄进入同一个 Presenter 命令。
- HUD 增加生命/经验条、武器/心诀图标槽、Boss 条和目标摘要；整页调试文本不再作为正式主交互。
- 可访问性继续保留文字、形状、焦点和高对比通道，不以颜色或动画单独表达关键信息。

### 5. 完成证据

- 资产数量、Catalog 数量、单帧首屏或 `activeViews=1` 不能再关闭表现里程碑。
- Player 必须连续运行至少 60 秒真实战斗并产生带时间点的截图/报告；证据同时包含玩家、手持/环绕武器、
  至少三类普通敌人、投射物或领域、正式 VFX、掉落/升级 UI 和 HUD。
- 2.5D 门禁必须验证倾斜相机、XZ 映射、地面/竖直层、阴影、深度排序和非文本按钮交互。

## 依赖方向

```text
Simulation 2D snapshots/events
          |
          v
PresentationSpace (x,y -> x,height,z) -- Camera/Map/View/VFX/UI rendering
          ^
          |
Infrastructure Addressables Owner -- formal sprite frames/catalogs
```

不改变 `Game.Core`、`Game.Content.Runtime`、`Game.Simulation`、`Game.Application` 的依赖方向和公开协议。
新增类型只位于 `Game.Presentation`、`Game.UI` 与最外层 `Game.Infrastructure`。

## 兼容、迁移与回滚

- Content Schema 6、Save Schema、30 Hz Tick、稳定 ContentId 和随机流不变。
- 旧 `VisualProfile` 单 Sprite 保留为首帧/降级入口；动画帧目录缺失时仍能显示静态正式 Sprite。
- 回滚可切回旧平面映射和文本选项，但不得删除新增门禁或再次以资源存在性代替实际战斗表现验收。

## 被拒绝的方案

- 只调整相机角度：不能解决地图平铺、Billboard、武器、动画、VFX 和 UI 缺失。
- 导入来源不明的 3D 模型或第三方 VFX 包：违反 provenance 与第三方审批规则。
- 每个敌人添加 Animator/Update：违反集中更新和高频实体预算。
- 继续用单帧截图与资产计数关闭里程碑：无法证明怪物、武器和战斗事件真实可见。

## 测试

- 托管程序集编译：Presentation、UI、Infrastructure、PlayMode Tests。
- EditMode：坐标映射、方向/帧选择、深度排序、正式资源解析和 UI 选项池。
- PlayMode：倾斜相机、地图层、玩家装备、敌人动画、正式 VFX、鼠标按钮与键盘/手柄同命令。
- Player：60 秒战斗视觉探针、多时间点截图、计数和窗口稳定性。
- Release：有效 Unity 环境下执行完整项目验证、Addressables、Windows Release Build 与 Player Smoke。
