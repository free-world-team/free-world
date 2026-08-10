# G3.5 未优化基线

## CPU Target

- 运行时间：2026-08-10
- 配置：54,000 Tick；1,500 Enemy；3,000 Projectile；5,000 Pickup；200 VFX
- 状态：`PASS`
- Tick average/p95/p99/max：16.686 / 17.511 / 18.036 / 22.642 ms
- 热路径托管分配：0 B；GC0/1/2：0/0/0
- 实体峰值：9,501；VFX 峰值/丢弃：200/0
- Checksum：`ca00e95592695395`
- 决策：EnemyDecision 平均 13.71 ms，是首要热点；总 Tick p99 仍小于 33.33 ms，按冻结规则不引入 Jobs/Burst。

机器可读原始报告：`cpu-target.json`。

## 1080p GPU Target

- 运行时间：2026-08-10；墙钟 1,811.7 秒
- 配置：54,000 Tick；1,200 Enemy；900 Projectile；500 Pickup；200 VFX；D3D11 1920×1080 Ultra
- 状态：`PASS`
- 107,986 Frame；平均 FPS / 1% Low：59.992 / 59.891
- Wall p99 / CPU p99 / GPU p99：16.697 / 16.717 / 2.163 ms
- Tick p99 / max：12.760 / 16.826 ms
- 热路径与 Profiler 分配：0 B；GC0/1/2：0/0/0
- 31 个内存样本；Managed 增长 0 B；Native 增长 83,596 B；无持续增长
- View / SpriteRenderer：2,801 / 5,402；扩容、丢弃、无效绑定：0/0/0
- Draw Call / SetPass / Triangle 峰值：21 / 20 / 22,906
- Checksum：`dbd76d45a0eeb892`
- 决策：全部强制预算直接通过，不执行无证据的表现降频或 Jobs/Burst 改造。

机器可读原始报告：`target-player.json`。
