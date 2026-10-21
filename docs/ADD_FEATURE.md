# 新增功能标准流程

本文档为 AI 与开发者提供**逐步 Checklist**。每步完成后再进行下一步。

---

## 1. 新增流程 Procedure

适用：新玩法阶段（大厅、战斗、结算等）。

### Checklist

- [ ] **1.1** 在 `Assets/MainGame/Scripts/Procedure/CustomProcedure/` 新建 `ProcedureXxx.cs`
- [ ] **1.2** 继承 `ProcedureFlowBase`（需要切场景+切流程）或 `ProcedureBase`（仅启动链内特殊流程）
- [ ] **1.3** 实现 `OnEnter` / `OnLeave` / `OnUpdate`（按需）
- [ ] **1.4** 在 `OnEnter` 中调用 `GameEntry.ProgressObserver?.OnProcedureEnter(typeof(ProcedureXxx))`（若使用 ProgressObserver）
- [ ] **1.5** 在 `OnLeave` 中对称调用 `OnProcedureLeave`
- [ ] **1.6** 若需打开上一流程传入的 UI：在 `OnEnter` 调用 `OpenUIFromData(procedureOwner)`
- [ ] **1.7** 在 `ProcedureBase.cs` 的 `ProcedureEnum` 添加新枚举值
- [ ] **1.8** 在 `ProcedureBase.ChangeToTargetProcedure` 的 `switch` 添加 `case ProcedureEnum.Xxx: ChangeState<ProcedureXxx>(...); break;`
- [ ] **1.9** Unity Editor：选中 GameFramework 物体 → Procedure Component → **Available Procedures** 勾选新流程
- [ ] **1.10** 若需新场景：先完成 [§5 场景表](#52-已有表新增行场景) 与场景文件，再用 `StartGame(ProcedureEnum.Xxx, SceneEnum.Xxx)` 跳转

### 模板（ProcedureFlowBase）

```csharp
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    public class ProcedureBattle : ProcedureFlowBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            GameEntry.ProgressObserver?.OnProcedureEnter(typeof(ProcedureBattle));
            OpenUIFromData(procedureOwner);
            // 初始化玩法...
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.ProgressObserver?.OnProcedureLeave(typeof(ProcedureBattle));
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            // m_HasPendingFlowTransition 为 true 时，基类会自动切 ProcedureChangeScene
        }

        public void ExitToMenu()
        {
            BackToMenu();
        }
    }
}
```

### 跳转到本流程（从其他流程）

```csharp
// 仅切流程+场景（场景名来自 DRScene）
StartGame(ProcedureEnum.战斗流程, SceneEnum.战斗场景);

// 切流程并指定 AssetName（不含路径与扩展名）
StartGame(ProcedureEnum.战斗流程, "Battle");

// 切流程并打开 UI（到目标流程后 OpenUIFromData 生效）
m_OpenUINextProcedure.Add(UIFormEnum.主界面);
StartGame(ProcedureEnum.菜单流程, SceneEnum.菜单场景);
```

---

## 2. 新增 UI 界面

适用：菜单、HUD、弹窗等。

### Checklist

- [ ] **2.1** 编辑 `Assets/MainGame/DataTables/Text/UIForm.txt`（或 Excel 工作表 `UIForm` → 菜单 `DataTables/A`）
- [ ] **2.2** 添加一行：Id、Name、AssetName、UIGroupName、AllowMultiInstance、PauseCoveredUIForm
- [ ] **2.3** 执行 `DataTables/B.生成数据表代码和byte文件`
- [ ] **2.4** 确认生成 `DRUIForm.cs` 中 `UIFormEnum` 含新项
- [ ] **2.5** 创建 Prefab：`Assets/MainGame/UI/UIForms/{AssetName}.prefab`
- [ ] **2.6** 创建脚本 `Assets/MainGame/Scripts/UI/{AssetName}.cs`，继承 `UGuiForm`
- [ ] **2.7** Prefab 根节点挂载脚本，绑定 SerializeField 引用
- [ ] **2.8** 若需打包/热更：打开 `Game Framework/Resource Tools/Resource Editor`，将 Prefab 分配到资源组（如 `ui`）并 Save（**禁止手改** `ResourceCollection.xml`；Editor 资源模式可跳过）
- [ ] **2.9** 打开界面：`GameEntry.UI.OpenUIForm(UIFormEnum.Xxx, userData)`
- [ ] **2.10** 关闭：`((UGuiForm)logic).Close()` 或 `GameEntry.UI.CloseUIForm(form)`；流程离开时建议对称关闭本流程打开的界面

### UIForm.txt 行示例

数据行格式（首列空 Tab + 策划备注列会被解析跳过）：

```
	100	主界面	主界面	MenuForm	Default	FALSE	FALSE
	101	设置界面	设置界面	SettingForm	Default	FALSE	TRUE
```

| 字段 | 说明 |
|------|------|
| Id | 唯一整数，对应 `UIFormEnum` |
| 策划备注 | 仅表内备注，运行时跳过 |
| Name | 显示/逻辑名称 |
| AssetName | Prefab 文件名（无 `.prefab`），须与脚本类名一致 |
| UIGroupName | 场景中已配置的 UI 组（如 Default、Popup、Game） |
| AllowMultiInstance | 是否允许多实例 |
| PauseCoveredUIForm | 是否暂停被覆盖的界面 |

参考实现：`MenuForm` + `ProcedureMenu`（进入菜单即打开、离开即关闭）。

### 模板（UGuiForm 子类）

```csharp
namespace MainGame
{
    public class MenuForm : UGuiForm
    {
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
        }

        public void OnClickPlaceholder()
        {
            // 占位：改为开始游戏等
        }
    }
}
```

流程内打开示例（见 `ProcedureMenu`）：

```csharp
if (!GameEntry.UI.HasUIForm(UIFormEnum.主界面))
{
    GameEntry.UI.OpenUIForm(UIFormEnum.主界面, this);
}
```

---

## 3. 新增实体 Entity

适用：角色、道具、特效逻辑实体等。

### Checklist

- [ ] **3.1** 编辑 `Assets/MainGame/DataTables/Text/Entity.txt`（或 Excel → 菜单 A）
- [ ] **3.2** 添加 Id、Name、AssetName
- [ ] **3.3** 执行 `DataTables/B.生成数据表代码和byte文件`
- [ ] **3.4** 创建 Prefab：`Assets/MainGame/Entities/{AssetName}.prefab`
- [ ] **3.5** Prefab 上挂载继承 `Entity` 的逻辑脚本
- [ ] **3.6** 创建 `XxxData : EntityData`（如需自定义字段）
- [ ] **3.7** 在 `EntityExtension` 添加 `ShowXxx` 扩展方法（或复用现有 private ShowEntity 模式）
- [ ] **3.8** 创建时指定实体组：`Constant.EntityGroup.Default` 等（世界 UI 用 `WorldUI` + `ShowWorldUI` / `DRWorldUI`）
- [ ] **3.9** 隐藏/销毁：`GameEntry.Entity.HideEntity(entity)` 或 `EntityExtension.HideEntity`

### Entity.txt 行示例

```
	1001	玩家	Player	Player
```

### 世界 UI（WorldUI 实体组）

世界飘字 / 进度条等走独立 `WorldUI` 表与 `WorldUI` 实体组，不要挂到 `UIComponent`：

1. 编辑 `DataTables/Text/WorldUI.txt` → `DataTables/B` 生成 `DRWorldUI` / `WorldUIEnum`
2. Prefab：`Assets/MainGame/Entities/WorldUI/{AssetName}.prefab`（逻辑类 `WorldUIEntity`）
3. 显示：`GameEntry.Entity.ShowWorldUI(WorldUIData.Create(...))` 或 `ShowWorldUIFloatingText(...)`
4. 摄像机：自定义主相机时调用 `WorldUIGroupCanvas.SetWorldCamera(camera)`；未绑定时回退 `Camera.main`

### EntityData + 显示（典型模式）

在 `EntityExtension.cs` 中增加 partial 方法：

```csharp
public static void ShowPlayer(this EntityComponent entityComponent, PlayerData data)
{
    entityComponent.ShowEntity(typeof(PlayerEntity), Constant.EntityGroup.Default,
        Constant.AssetPriority.Player, data);
}
```

`ShowEntity` 内部会查 `DREntity` 表解析 Prefab 路径。

### 生命周期注意

- `EntityData` 使用 `ReferencePool.Acquire<T>()` 创建
- 实体隐藏时在 `Entity.ReleaseData()` 中 `ReferencePool.Release(m_EntityData)`

---

## 4. 新增网络协议

适用：客户端与服务器通信新消息。

### Checklist

- [ ] **4.1** 分配唯一整数 **Packet Id**（与现有 `CSHeartBeat=1`, `SCHeartBeat=2` 不冲突）
- [ ] **4.2** 客户端发送：在 `Network/Packet/` 新建 `CSXxx.cs`，继承 `CSPacketBase`，实现 `Id` 与 `Clear()`
- [ ] **4.3** 服务端下发：在 `Network/Packet/` 新建 `SCXxx.cs`，继承 `SCPacketBase`
- [ ] **4.4** 在 `Network/PacketHandler/` 新建 `SCXxxHandler.cs`，继承 `PacketHandlerBase`，`Id` 与 SC 包一致
- [ ] **4.5** 字段用 ProtoBuf 标注 `[ProtoMember(n)]`
- [ ] **4.6** 发送：`GameEntry.Network.GetNetworkChannel(...).Send(new CSXxx { ... })`
- [ ] **4.7** 无需手动注册 Handler — `NetworkChannelHelper` 启动时反射注册

### 模板

```csharp
// Network/Packet/SCPlayerInfo.cs
[Serializable, ProtoContract(Name = @"SCPlayerInfo")]
public class SCPlayerInfo : SCPacketBase
{
    public override int Id => 100;

    [ProtoMember(1)] public int PlayerId { get; set; }
    [ProtoMember(2)] public string Name { get; set; }

    public override void Clear()
    {
        PlayerId = 0;
        Name = null;
    }
}

// Network/PacketHandler/SCPlayerInfoHandler.cs
public class SCPlayerInfoHandler : PacketHandlerBase
{
    public override int Id => 100;

    public override void Handle(object sender, Packet packet)
    {
        SCPlayerInfo msg = (SCPlayerInfo)packet;
        // 处理逻辑...
    }
}
```

---

## 5. 新增配置表

适用：全新 DataTable（不仅是已有表加行）。

### Checklist

- [ ] **5.1** 在 Excel 新建工作表（名称 = 输出文件名，如 `Item`）或新建 `Text/Item.txt`
- [ ] **5.2** 按格式填写表头行（`#` 注释 + 列类型 + 列说明）
- [ ] **5.3** 在 `ProcedurePreload.DataTableNames` 数组**末尾**添加 `"Item"`
- [ ] **5.4** 执行 `DataTables/B.生成数据表代码和byte文件`
- [ ] **5.5** 确认生成 `DRItem.cs` 与 `Bytes/Item.bytes`
- [ ] **5.6** 若需打包/热更：在 Resource Editor 中分配 bytes 对应资源并 Save（禁止手改 `ResourceCollection.xml`）
- [ ] **5.7** 运行时访问：`GameEntry.DataTable.GetDataRow<DRItem>(id)`

### 5.2 已有表新增行（场景）

编辑 `Scene.txt`：

```
	3	战斗场景	战斗场景	Battle
```

生成后 `SceneEnum.战斗场景` 可用，`StartGame(..., SceneEnum.战斗场景)` 会加载 `Assets/MainGame/Scenes/Battle.unity`。

- [ ] 创建场景文件 `Assets/MainGame/Scenes/Battle.unity`
- [ ] 加入 File → Build Settings

---

## 6. 新增进度观察器 ProgressObserver

适用：流程内任务/引导/可交互观察点。

### Checklist

- [ ] **6.1** 在 `ProgressObserverIds` 或约定处定义唯一字符串 Id
- [ ] **6.2** 创建 `XxxObserverEntity : ProgressObserverEntity`
- [ ] **6.3** 添加 `[ProgressObserver("id", SpawnMode, Signal, typeof(ProcedureXxx), ...)]`
- [ ] **6.4** 实现观察逻辑（触发 `MarkCompleted` 等）
- [ ] **6.5** 确保目标 Procedure 的 `OnEnter/OnLeave` 调用了 ProgressObserver
- [ ] **6.6** 完成态查询：`GameEntry.ProgressObserver.IsCompleted(id)`

### 模板

```csharp
[ProgressObserver("tutorial.move", ProgressObserverSpawnMode.ProcedureEnter,
    ProgressObserverSignal.None, typeof(ProcedureMenu))]
public sealed class MoveTutorialObserver : ProgressObserverEntity
{
    // 实现交互与 MarkCompleted("tutorial.move")
}
```

实体由 `ProgressObserverComponent` 通过 `EntityExtension.ShowProgressObserverEntity` 创建，使用空实体 Prefab。

---

## 7. 新增平台能力

适用：微信/抖音小游戏等平台差异。

### Checklist

- [ ] **7.1** 在 `Platform/` 新建 `XxxPlatformService.cs`，继承 `PlatformServiceBase`，实现 `IPlatformService`
- [ ] **7.2** 在 `PlatformComponent` 中按 `PlatformType` 或编译宏注册实现
- [ ] **7.3** 资源路径差异走 `ResolveResourcePath` 等平台方法，不在业务中散落 `#if`
- [ ] **7.4** 可选：在 `PlatformFeature` 添加特性枚举
- [ ] **7.5** Editor：`PlatformComponentInspector` 如需新字段则扩展

---

## 8. 新增 Custom 组件

适用：新的全局 `GameFrameworkComponent`。

### Checklist

- [ ] **8.1** 创建 `XxxComponent : GameFrameworkComponent`
- [ ] **8.2** 挂到 `GameEntryScene` 的 GameFramework 物体
- [ ] **8.3** 在 `GameEntry.Custom.cs` 添加 `public static XxxComponent Xxx { get; private set; }`
- [ ] **8.4** 在 `InitCustomComponents()` 中赋值
- [ ] **8.5** 其他模块通过 `GameEntry.Xxx` 访问

---

## 9. 新增输入动作

适用：Gameplay 新按键/摇杆动作。配置文件：`Assets/MainGame/Configs/GameControls.inputactions`。

### Checklist

- [ ] **9.1** 在 `Configs/GameControls.inputactions` 的 Gameplay Action Map 添加 Action，绑定挂到对应 Control Scheme
- [ ] **9.2** 在 `.inputactions` Inspector 上 **Generate C# Class**（更新 `Scripts/Control/GameControls.cs`）
- [ ] **9.3** 玩法侧经 `GameEntry.Control`：`GetAction*` / `GetMoveVector`，动作用 `Controls.Gameplay.Xxx`（勿直接读旧 Input API）

---

## 10. 验证清单（通用）

完成任意新增后：

- [ ] Unity Console 无编译错误
- [ ] 运行 `GameEntryScene`，流程能走到目标 Procedure
- [ ] 新 UI/Entity/表数据在运行时能加载（Editor 资源模式 / 打包模式各测一次）
- [ ] 若改 Procedure：Available Procedures 已勾选
- [ ] 若改 DataTable：txt、bytes、DR*.cs 三者已同步生成并提交
- [ ] 更新相关文档（枚举说明、README 场景表等）

---

## 快速参考：Editor 菜单顺序

1. `DataTables/A.Excel导出数据表文本` — 改了 Excel 时先执行
2. `DataTables/B.生成数据表代码和byte文件` — 改了 txt 或 A 之后执行
3. 运行 `GameEntryScene` 验证
