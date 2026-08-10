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
