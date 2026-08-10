# G3.5 目标硬件性能冻结

## 1. 目标与范围

G3.5 在 G3.4 Pack `0.10.0` 和正式视觉、音频、字体、本地化全部启用的候选上，关闭 CPU、真实 GPU、
1% Low、稳态 GC、内存趋势、池容量和 30 分钟 Soak 证据。优化只允许改变表现层调度、采样和池策略，
不得改变 30 Hz 模拟、命中次数、敌人数量、Content Schema、存档或确定性随机流。

本工作包不生成 Release Build，不签署商店/法律/第三方合规，也不执行干净克隆 Release Candidate；这些只属于
G3.6。

## 2. 冻结硬件与运行配置

本轮可用的 Demo 参考目标机冻结为：

| 项目 | 值 |
|---|---|
| OS | Windows 11 Pro 64-bit，10.0.26200 |
| CPU | Intel Core i7-12700F，12 Core / 20 Thread |
| GPU | NVIDIA GeForce RTX 3060 Ti，驱动 32.0.15.9186 |
| RAM | 32 GB |
| 显示 | 2560×1440 桌面；Player 强制 1920×1080 Windowed |
| 图形 API | Direct3D 11 |
| Unity | 6000.3.20f1 |
| Quality | `Ultra`；VSync Off；目标 60 FPS |

`Templates/PROJECT_VARIABLES.md` 中 4 核、8 GB、DX11、2 GB VRAM 仍是初始最低兼容目标；本机结果只证明
本参考目标机，不伪造最低规格实机认证。G3.6 发布说明必须保留这一区别。

## 3. 三层性能证据

### 3.1 正式内容 CPU/容量 Soak

沿用生产稠密 Store 和 M10 Stress Harness，以 `qinglan.enemy.grass_spirit` 替代测试敌人，运行 54,000 Tick：

- 1,500 Enemy、3,000 Projectile、5,000 Pickup、200 VFX；
- 300 Tick 预热不计入分位；
- Tick average/p95/p99/max、分系统时间、内存趋势、GC、容量、诊断和 Checksum 全部落 JSON；
- Tick p99 ≤33.33 ms、热路径 0 B、GC 0、持续增长为 false、容量和诊断零异常。

该层是 CPU/容量门禁，不冒充 GPU 证据。

### 3.2 1080p 正式 GPU 30 分钟 Player Soak

Development Player 新增 opt-in `-qinglanG35Performance` 驱动。它必须使用 Addressables 中已加载的正式
Catalog 和正式 Sprite，预热后以 30 Hz 推进 54,000 Tick、以 60 FPS 呈现约 108,000 Frame：

- 1,200 Formal Enemy、900 Formal Projectile/Area、500 Formal Pickup、200 Formal VFX；
- UI 背景、地图、正式字体与正式音频保持加载；
- 使用 `FrameTimingManager` 记录 wall/cpu/gpu frame，使用预分配数组记录模拟 Tick；
- 每模拟分钟记录 Mono/Native/GC Heap，记录显存、Draw Call、SetPass、三角形和池指标；
- 输出硬件、驱动、Git SHA、Pack Hash、分辨率、Quality、Seed、Checksum 和完整预算判定。

强制门槛：平均 FPS ≥59、1% Low ≥45、GPU p99 ≤16.67 ms、Tick p99 ≤33.33 ms、稳态 GC 0 B/frame、
测量窗 GC 0、无持续内存增长、预热后无池扩容/失败、无无效 Handle/VFX 丢弃。

### 3.3 2,000 Enemy 扩展压力

同一 Player 另运行 9,000 Tick / 5 分钟、2,000 Formal Enemy 的扩展压力。该项不是 Demo 发布必过项，但
必须输出 `PASS` 或 `FAIL`；不得省略或写成历史继承。

## 4. 测量真实性

- Player 不使用 `-nographics`，强制 `-force-d3d11 -screen-width 1920 -screen-height 1080`；
- 只有 `FrameTimingManager.GetLatestTimings` 返回有效 GPU 样本时，GPU 项才可判定；缺样本为 `FAIL`；
- 预热、测量和清理分段，初次 Addressables/Shader/池创建不混入稳态窗口；
- 百分位在预分配数组上于结束后排序，测量帧不使用 LINQ、字符串格式化或临时集合；
- 任何崩溃、窗口失焦、远程显示适配器接管、分辨率漂移或结果 JSON 缺失均为 `FAIL`；
- 记录所有失败基线；优化后必须同硬件、同 Seed、同配置复测，不能只保留最好一次。

## 5. 优化顺序

只有基线证明超预算才按以下顺序处理：

1. 远处敌人动画降频；
2. 远处受击闪烁降频；
3. 伤害数字聚合；
4. 非关键 VFX 采样；
5. 屏外 View 低频更新；
6. 同 Sprite 合批或 Instancing。

EnemyDecision 仍是已知 CPU 热点，但在目标 p99 内不得提前引入 Jobs/Burst。若必须改变程序集/API、Tick 或
Content Schema，应停止 G3.5 并先提交 ADR/Change Request。

## 6. 提交顺序

| 步骤 | 交付 | 提交边界 |
|---:|---|---|
| 1 | 本设计、硬件、门槛与测量真实性冻结 | 文档单独提交并 Push |
| 2 | Player Runner、报告 DTO、脚本与聚焦测试 | 实现单独提交并 Push |
| 3 | 未优化基线与失败分析 | 证据单独提交并 Push |
| 4 | 必要的单项优化 | 每项优化分别提交并 Push |
| 5 | 54,000 Tick、30 分钟 GPU、2,000 扩展、全量回归、Build 与报告 | 最终证据单独提交并 Push |

## 7. 强制验证

| 检查 | 最低要求 |
|---|---|
| G3.5 聚焦 EditMode | 报告判定、百分位、硬件/配置、缺 GPU 样本失败合同 `PASS` |
| 1,500/3,000/5,000 CPU Soak | 54,000 Tick `PASS` |
| 1080p Player Soak | 54,000 Tick、平均/1% Low/GPU/GC/池/内存全部 `PASS` |
| 2,000 Enemy 扩展 | 实际运行并记录 `PASS` 或 `FAIL` |
| 全量 EditMode / PlayMode | `PASS` |
| Project Validation | `PASS` |
| Windows Development Build / Player Smoke | `PASS` |

任一强制项为 `FAIL` 或 `NOT RUN` 时，G3.5 只能是 `INCOMPLETE`，不得进入 G3.6。
