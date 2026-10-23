# Shard-Shot 俯视角射击 — 开发计划

> 面试测试项目 · 开发周期 3 天 · 核心目标：**动感 + Juice + 物理混乱**

---

## 1. 项目概述

### 1.1 题目要求

将游戏尽量做得动感，突出 **Juice**（打击感、反馈、爽感）。其余无硬性约束。

### 1.2 玩法定位

**俯视角射击（Top-Down Shooter）**：玩家在一个空场地中操控角色，使用三种武器击杀脆弱的机器人敌人。敌人死亡后按配置碎裂为物理刚体块，形成持续变化的「混乱战场」——碎片可被子弹击飞、被爆炸推开、被近战扫飞，并可能对玩家造成二次伤害。

### 1.3 核心体验

> 玩家在一个空场地里用三种武器打碎机器人，场地会越来越「乱」；混乱本身既是威胁，也是爽感来源。

### 1.4 评审展示重点

| 维度 | 目标 |
|------|------|
| Juice | 开火 / 命中 / 击杀 / 爆炸均有视听反馈叠加 |
| 物理混乱 | 子弹、爆炸、近战均可与残骸互动，战场状态持续变化 |
| 技术展示 | 架构清晰、系统可扩展、符合 UGF 项目规范 |
| 交付物 | 3 天可玩 Demo + 流畅录屏 |

---

## 2. 设计目标

### 2.1 武器分工

| 武器 | 核心体验 | Juice 亮点 |
|------|----------|------------|
| **加特林** | 持续射击、射速爬升、弹匣换弹 | 射速越高子弹越亮、拖尾越长；高击退；可清扫碎片 |
| **炸弹枪** | 范围爆炸、物理击退 | 飞行呼啸 → 爆炸闪白 + 强震 + 烟尘扩散 |
| **近战** | 扇形挥击、强击退 | 可击打已死亡机器人的碎片，制造「扫场」爽感 |

### 2.2 敌人设计

- **脆弱机器人**：低血量（约 1～3 发致死），受击闪白、小幅击退
- **死亡碎裂**：按 `RagdollProfile` 配置，刚体块飞散
- **AI**：极简即可（朝玩家移动 / 定时刷怪），不投入复杂行为树

### 2.3 非目标（Scope 外）

- 多人 / 网络对战
- 复杂关卡与剧情
- 多种敌人类型与 Boss
- 完整 Meta 进度（升级、商店等）

---

## 3. 技术约束（项目规范）

业务代码仅修改 `Assets/MainGame/`，须遵守现有架构约定：

| 事项 | 规范 |
|------|------|
| 切场景 | `ProcedureFlowBase.StartGame` → `ProcedureChangeScene`，禁止 `SceneManager.LoadScene` |
| 实体 | `Entity` + `EntityData` + `EntityExtension.ShowXxx` |
| UI | `GameEntry.UI.OpenUIForm(UIFormEnum.*)` |
| 输入 | `GameEntry.Control`，扩展 `Configs/GameControls.inputactions` |
| 资源路径 | `AssetUtility.*`，禁止硬编码 `Assets/MainGame/...` |
| 配置表 | 改 txt/Excel → `DataTables/B`，禁止手改 `DR*.cs` |
| 新 Procedure | `ProcedureEnum` + `ChangeToTargetProcedure` + Inspector Available Procedures |

详细 Checklist 见 [ADD_FEATURE.md](ADD_FEATURE.md)，架构见 [ARCHITECTURE.md](ARCHITECTURE.md)。

---

## 4. 技术选型

| 系统 | 方案 | 说明 |
|------|------|------|
| 视角 | 正交 / 小角度透视 + Cinemachine | 俯视角清晰；Impulse 实现相机震动 |
| 玩家移动 | `CharacterController` 或冻结 Y 旋转的 `Rigidbody` | 俯视角 XZ 平面移动 |
| 瞄准 | 鼠标 / 右摇杆 → 世界坐标朝向 | 复用 `GetLookVector` |
| 敌人（存活） | Kinematic `Rigidbody` + 胶囊碰撞 | 易击退、性能稳定 |
| 死亡碎裂 | 预制子物体 `Rigidbody` 块 + `RagdollProfile` | 配置驱动，美术可调 |
| 子弹 | 对象池 + `Rigidbody` 或 Raycast 连续检测 | 需与碎片持续交互 |
| 爆炸 | `Physics.OverlapSphere` + `AddExplosionForce` | 统一处理敌人与碎片 |
| Juice | `JuiceComponent`（`GameEntry` 扩展） | 全局震屏、HitStop、反馈调度 |
| 数值配置 | ScriptableObject（3 天优先） | 武器/敌人参数；场景/实体/UI 走 DataTable |

---

## 5. 系统架构

### 5.1 模块关系

```
ProcedureBattle
    └── BattleManager（战斗状态、胜负）
            ├── SpawnManager（刷怪）
            ├── PlayerEntity
            │       └── WeaponController
            │               ├── GatlingWeapon
            │               ├── BombLauncherWeapon
            │               └── MeleeWeapon
            ├── EnemyEntity → RagdollController → DebrisBehaviour
            ├── DamageSystem（统一伤害入口）
            ├── PhysicsForceSystem（击退、爆炸力、碎片冲量）
            ├── JuicePresenter（相机震、HitStop、特效触发）
            └── AudioPresenter（音效分层）
```

### 5.2 目录规划

```
Assets/MainGame/Scripts/
├── Battle/
│   ├── ProcedureBattle.cs
│   ├── BattleManager.cs
│   ├── SpawnManager.cs
│   ├── DamageSystem.cs
│   ├── PhysicsForceSystem.cs
│   ├── Juice/
│   │   ├── JuiceComponent.cs
│   │   ├── CameraShake.cs
│   │   └── HitStop.cs
│   ├── Weapon/
│   │   ├── IWeapon.cs
│   │   ├── WeaponBase.cs
│   │   ├── GatlingWeapon.cs
│   │   ├── BombLauncherWeapon.cs
│   │   ├── MeleeWeapon.cs
│   │   └── WeaponController.cs
│   └── Ragdoll/
│       ├── RagdollProfile.cs          # ScriptableObject
│       ├── RagdollController.cs
│       └── DebrisBehaviour.cs
├── Entity/
│   ├── PlayerEntity.cs / PlayerData.cs
│   ├── EnemyEntity.cs / EnemyData.cs
│   ├── BulletEntity.cs / BulletData.cs
│   └── ExplosionEntity.cs
└── Utility/Battle/
    └── BattleUtility.cs               # 伤害公式、层检测等纯函数
```

### 5.3 伤害管线

所有武器、碎片、爆炸统一走 `DamageSystem.ApplyDamage(in DamageInfo info)`：

1. 结算伤害（护甲、部位倍率可后加）
2. 击退 / 爆炸力 → `PhysicsForceSystem`
3. 若致死 → `EnemyEntity.OnDeath(ragdollImpulse)`
4. 广播事件 → `JuicePresenter`（震屏、闪白、HitStop）
5. 广播事件 → `AudioPresenter`（按武器 / 命中类型播音效）

**DamageInfo 建议字段**

| 字段 | 类型 | 说明 |
|------|------|------|
| `Source` | `GameObject` | 伤害来源 |
| `Target` | `IDamageable` | 受伤目标 |
| `Amount` | `float` | 伤害量 |
| `HitPoint` | `Vector3` | 命中点 |
| `HitNormal` | `Vector3` | 命中法线 |
| `Impulse` | `float` | 击退强度 |
| `DamageType` | `enum` | Bullet / Explosion / Melee / Debris |
| `CanKill` | `bool` | 是否可致死 |

---

## 6. 物理与碰撞分层

### 6.1 Layer 定义

| Layer | 用途 |
|-------|------|
| `Player` | 玩家 |
| `Enemy` | 存活机器人 |
| `Debris` | 死亡后刚体碎片 |
| `Projectile` | 子弹 |
| `Environment` | 场地边界、障碍 |

### 6.2 碰撞矩阵要点

- `Projectile` ↔ `Enemy` / `Debris` / `Environment`：开启
- `Debris` ↔ `Debris`：建议关闭或降低（性能）
- `Debris` ↔ `Player` / `Enemy`：开启（碎片伤害检测）

### 6.3 碎片伤害

`DebrisBehaviour.OnCollisionEnter`：相对速度超过阈值 → 对 `Player` / `Enemy` 造成 `DamageType.Debris` 伤害。同一碎片对同一目标带冷却，防止抖动连伤。

### 6.4 性能护栏

- 全场碎片上限约 **80 块**，超出回收最老碎片
- 子弹、粒子、敌人使用对象池
- 高速子弹使用 Raycast 连续检测，避免穿透

---

## 7. 核心玩法规格

### 7.1 玩家（PlayerEntity）

| 能力 | 输入 | 实现 |
|------|------|------|
| 移动 | WASD / 左摇杆 | `GetMoveVector` |
| 瞄准 | 鼠标 / 右摇杆 | 世界坐标朝向，`transform.forward` 对齐 |
| 开火 | 鼠标左键 / RT | `WeaponController.Fire` |
| 换弹 | R / X | `GatlingWeapon.Reload` |
| 换枪 | 1/2/3 或滚轮 | `WeaponController.Switch` |

受击反馈：短暂无敌 + 屏幕红边 + 相机震动。

### 7.2 敌人死亡碎裂流程

```
EnemyEntity.OnDeath(impulse)
  1. 禁用存活碰撞体与 AI
  2. 显示 Ragdoll 子节点（每块独立 Rigidbody + Collider）
  3. 按 RagdollProfile 施加初速度（击杀方向 + 随机扰动）
  4. 碎片切到 Debris 层，挂载 DebrisBehaviour
  5. 延迟 N 秒后淡出回收（或达到全场上限时强制回收）
```

**RagdollProfile 配置项**

| 项 | 说明 |
|----|------|
| `pieceCount` | 碎块数量（建议 5～8） |
| `disperseForce` | 飞散基础力 |
| `lifetime` | 存活时间 |
| `minDamageSpeed` | 碎片伤人速度阈值 |
| 每块 `mass` / `drag` | 单块物理参数 |

> 不必使用 Humanoid Ragdoll；机器人用立方体 / 零件块即可，强调配置驱动。

### 7.3 碎片交互矩阵

| 作用源 | 对碎片行为 |
|--------|------------|
| 加特林子弹 | `AddForceAtPosition`，高速击飞 |
| 炸弹爆炸 | `AddExplosionForce`，范围推开 |
| 近战挥击 | 扇形范围内施加冲量 |

---

## 8. 武器详细设计

### 8.1 加特林（Gatling）

| 参数 | 建议初值 |
|------|----------|
| 初始射速 | 8 发/秒 |
| 最大射速 | 20 发/秒 |
| 射速爬升 | 连射约 2s 爬满 |
| 弹匣容量 | 60 |
| 换弹时间 | 1.8s |
| 单发伤害 | 低 |
| 击退 | 高 |

**射速热力学**

```
fireRate = Lerp(baseRate, maxRate, heat01)
heat01  += fireHeatPerShot    // 开火时上升
heat01  -= coolRate * dt      // 停火时下降
```

**Juice 联动（随 heat01 变化）**

- 子弹 `TrailRenderer` 长度增加
- 粒子亮度 / 颜色偏暖
- 枪口火光频率加快
- 音高 `pitch = Lerp(0.9, 1.3, heat01)`
- 连射时轻微持续相机微震

### 8.2 炸弹枪（Bomb Launcher）

| 参数 | 建议初值 |
|------|----------|
| 射速 | 0.5～1 发/秒 |
| 弹道 | 抛物线刚体或直线慢弹 |
| 爆炸半径 | 4～6m |
| 伤害 | 中心高、边缘衰减 |
| 爆炸力 | 向上分量略大，强调「炸飞」 |

**Juice**：飞行呼啸 → 爆炸闪白 + 强震 + 可选 HitStop（0.05s）+ 烟尘环扩散。

### 8.3 近战（Melee）

| 参数 | 建议初值 |
|------|----------|
| 攻击间隔 | 0.4～0.6s |
| 范围 | 前方 120° 扇形，半径 2m |
| 检测 | `Physics.OverlapSphere` + 角度过滤 |
| 击退 | 强径向力 + 少量上抛 |

**特色**：挥击同时对 `Enemy` 与 `Debris` 生效，可「扫」清场地碎片。

---

## 9. Juice 系统

### 9.1 必做（D2 末～D3）

| 效果 | 实现 | 触发时机 |
|------|------|----------|
| 相机震动 | Cinemachine Impulse / Perlin 偏移 | 开火(轻)、命中(中)、爆炸(重) |
| 命中反馈 | 火花粒子 + 敌人闪白 | `OnDamage` |
| 击杀反馈 | 碎片爆散 + 短震 + 击杀音 | `OnDeath` |
| 枪口火光 | 简单 Particle | 开火 |
| HUD | 血量、弹药、武器、加特林热条 | `BattleHudForm` |

### 9.2 选做（时间有余）

- HitStop：`Time.timeScale` 短暂降至 0.05，持续 0.03～0.05s
- 受击屏幕 Vignette 闪红
- 连杀 / 碎片计数 UI
- 地面弹坑 Decal（URP Decal Projector）

### 9.3 音频分层

| 类别 | 做法 |
|------|------|
| 武器 | 每枪 2～3 个 one-shot 随机轮换 |
| 碎片碰撞 | 速度映射音量，限流防爆音 |
| UI | 换枪、换弹、空弹 click |
| 环境 | 低频战场底噪（可选） |

使用 `GameEntry.Sound` + `SoundExtension`，音效 ID 走 `DRSound` 表。

---

## 10. 数据与配置

### 10.1 DataTable（必须）

| 表 | 新增内容 |
|----|----------|
| `Scene.txt` | `Battle` 战斗场景 |
| `Entity.txt` | `Player`、`EnemyBot`、`Bullet`、`ExplosionFx` |
| `UIForm.txt` | `BattleHudForm` |
| `ProcedureEnum` | `战斗流程`（手写，`ProcedureBase.cs`） |

新增表后须加入 `ProcedurePreload.DataTableNames`（若新建表）。

### 10.2 ScriptableObject（3 天优先）

| 资产 | 内容 |
|------|------|
| `GatlingConfig` | 射速、弹匣、伤害、击退、热力学参数 |
| `BombLauncherConfig` | 射速、爆炸半径、伤害曲线、力 |
| `MeleeConfig` | 间隔、范围、角度、伤害、击退 |
| `EnemyBotConfig` | 血量、移动速度、碎裂 Profile 引用 |
| `RagdollProfile` | 碎块物理与飞散参数 |

后续扩展可迁移为 `Weapon.txt` / `Enemy.txt` 配置表。

---

## 11. 新增资源清单

### 11.1 场景

- `Assets/MainGame/Scenes/Battle.unity` — 空场地 + 边界 + 主相机 + Cinemachine

### 11.2 Prefab

| Prefab | 路径 |
|--------|------|
| `Player` | `Entities/Player.prefab` |
| `EnemyBot` | `Entities/EnemyBot.prefab`（含 Ragdoll 子节点） |
| `Bullet` | `Entities/Bullet.prefab` |
| `ExplosionFx` | `Entities/ExplosionFx.prefab` |
| `BattleHudForm` | `UI/UIForms/BattleHudForm.prefab` |

### 11.3 输入扩展（GameControls.inputactions）

Gameplay Action Map 新增：

- `Fire` — 开火
- `Reload` — 换弹
- `Weapon1` / `Weapon2` / `Weapon3` — 换枪
- `Aim` — 瞄准（若与 Look 分离）

---

## 12. 三天开发计划

### Day 1 — 可玩骨架

**目标**：能走、能射、敌人能死。

| 任务 | 验收标准 |
|------|----------|
| `ProcedureBattle` + `Battle.unity` | 菜单 → 战斗场景跳转正常 |
| `PlayerEntity` 移动 + 瞄准 | 俯视角控制流畅 |
| `EnemyEntity` + `SpawnManager` | 场地中出现敌人 |
| `DamageSystem` 基础 | 子弹命中扣血 |
| `GatlingWeapon` 固定射速版 | 单武器可击杀敌人 |
| `MenuForm` 开始游戏按钮 | 点击进入战斗 |

**D1 里程碑**：单武器射击 Demo 可玩。

### Day 2 — 物理混乱

**目标**：碎裂 + 三武器 + 碎片互动。

| 任务 | 验收标准 |
|------|----------|
| `RagdollController` + `DebrisBehaviour` | 击杀后刚体块飞散 |
| 子弹对碎片施力 | 扫射可使碎片位移 |
| 加特林完整版 | 射速爬升、弹匣、换弹、拖尾/亮度变化 |
| `BombLauncherWeapon` | 爆炸击退敌人与碎片 |
| `MeleeWeapon` | 扇形攻击 + 碎片击打 |
| `WeaponController` 三枪切换 | 1/2/3 或滚轮切换 |

**D2 里程碑**：三武器 + 碎片物理互动完整。

### Day 3 — Juice 与交付

**目标**：爽感拉满、可提交。

| 任务 | 验收标准 |
|------|----------|
| `JuiceComponent` | 相机震、命中特效、击杀反馈 |
| `BattleHudForm` | 血量、弹药、武器、热条 |
| 音频 pass | 开火/命中/爆炸/换弹有音效，无爆音 |
| 性能优化 | 对象池、碎片上限、无明显卡顿 |
| Bug 修复 + 录屏 | 稳定可演示 |

**D3 里程碑**：可提交面试的完整 Demo。

---

## 13. 风险与砍 Scope 策略

| 风险 | 对策 |
|------|------|
| 物理碎片过多卡顿 | 碎片上限 + 池化 + 关闭碎片间碰撞 |
| AI 时间不足 | 定时刷怪 + 直线追击 |
| Ragdoll 难调 | 仅 1 种机器人，固定 6 块 |
| 子弹穿透 | Raycast 连续检测 |
| 碎片伤害难平衡 | 碎片对玩家小额伤害，主打紧张感而非秒杀 |

**砍 Scope 优先级**（从先到后）：

1. 近战特效细节
2. 炸弹抛物线（改直线慢弹）
3. 加特林射速视觉（保留机制）
4. 碎片对玩家伤害（保留被击飞）

---

## 14. 实现 Checklist（按依赖顺序）

### 14.1 基础设施

- [ ] `ProcedureEnum.战斗流程` + `ProcedureBattle.cs`
- [ ] `ChangeToTargetProcedure` 添加 case
- [ ] Inspector Available Procedures 勾选
- [ ] `Scene.txt` 添加 Battle + 创建场景 + Build Settings
- [ ] `MenuForm` 开始游戏 → `StartGame(ProcedureEnum.战斗流程, SceneEnum.战斗场景)`

### 14.2 战斗核心

- [ ] `Entity.txt` 注册 Player / EnemyBot / Bullet
- [ ] `PlayerEntity` + `PlayerData` + `EntityExtension.ShowPlayer`
- [ ] `EnemyEntity` + `EnemyData` + `EntityExtension.ShowEnemy`
- [ ] `DamageSystem` + `DamageInfo` + `IDamageable`
- [ ] `BattleManager` + `SpawnManager`
- [ ] 扩展 `GameControls.inputactions`（Fire / Reload / WeaponSwitch）

### 14.3 武器

- [ ] `IWeapon` + `WeaponBase` + `WeaponController`
- [ ] `GatlingWeapon`（含热力学、弹匣、换弹）
- [ ] `BombLauncherWeapon` + `ExplosionEntity`
- [ ] `MeleeWeapon`

### 14.4 物理碎裂

- [ ] `RagdollProfile` ScriptableObject
- [ ] `RagdollController` + `DebrisBehaviour`
- [ ] Layer 配置与碰撞矩阵
- [ ] `PhysicsForceSystem`

### 14.5 Juice 与 UI

- [ ] `JuiceComponent` 注册到 `GameEntry.Custom.cs`
- [ ] 相机震动、命中特效、击杀反馈
- [ ] `UIForm.txt` + `BattleHudForm`
- [ ] 音效接入（`DRSound`）

### 14.6 验证

- [ ] `GameEntryScene` 启动无报错
- [ ] 菜单 → 战斗 → 三武器可切换
- [ ] 击杀碎裂、碎片可被击打
- [ ] Console 无持续错误日志
- [ ] 录屏 1～2 分钟展示 Juice

---

## 15. Demo 叙述提纲（面试 2 分钟）

1. **设计意图**：俯视角 + 可交互残骸，让战场状态成为玩法的一部分。
2. **技术亮点**：统一 `DamageSystem`、武器策略模式、`RagdollProfile` 配置化、Juice 与逻辑解耦。
3. **Juice 案例**：加特林热条联动音画；爆炸统一力场驱动敌人与碎片。
4. **工程规范**：UGF Procedure 切场景、Entity 生命周期、输入与资源路径约定。

---

## 16. 相关文档

| 文档 | 说明 |
|------|------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | 模块架构与数据流 |
| [ADD_FEATURE.md](ADD_FEATURE.md) | 新增 Procedure / UI / Entity Checklist |
| [CONVENTIONS.md](CONVENTIONS.md) | 命名与编码约定 |
| [../AGENTS.md](../AGENTS.md) | AI 协作与禁止事项 |
| [../README.md](../README.md) | 项目启动说明 |

---

*文档版本：v1.0 · 与仓库模板 UGF 26.8 对齐*
