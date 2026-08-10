# G3.5 最终证据

## 2,000 Enemy 扩展压力

- 运行时间：2026-08-10；墙钟 311.8 秒
- 配置：9,000 Tick；2,000 Enemy；900 Projectile；500 Pickup；200 VFX；D3D11 1920×1080 Ultra
- 观察状态：`FAIL`（非 Demo 发布必过项）
- 失败预算：平均 FPS 43.694 < 59；1% Low 36.768 < 45
- CPU frame average/p99：22.885 / 27.197 ms
- Tick average/p99：22.062 / 23.685 ms；仍小于 33.33 ms Tick 预算
- GPU average/p99：2.351 / 2.822 ms；GPU 不是瓶颈
- 热路径与 Profiler 分配：0 B；GC0/1/2：0/0/0；无持续内存增长
- View / SpriteRenderer：3,601 / 7,002；扩容、丢弃、无效绑定：0/0/0
- Checksum：`14988f4e65e013e2`
- 决策：该项是非发布门禁的容量观察，正式 1,200 Enemy Target 已 PASS；不修改冻结玩法，不引入 Jobs/Burst。

机器可读原始报告：`extension-player.json`。
