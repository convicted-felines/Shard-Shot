# 编码与目录约定

## 1. 命名空间

| 区域 | 命名空间 |
|------|----------|
| 运行时业务 | `MainGame` |
| Editor 通用 | `MainGame.Editor` |
| Editor 配置表 | `MainGame.Editor.DataTableTools` |

不要使用 `Invector` 等遗留命名空间；新代码统一 `MainGame`。

## 2. 目录约定

```
Assets/MainGame/
├── Configs/           # UGF 资源 XML + GameControls.inputactions
├── DataTables/        # Excel / Text / Bytes 配置表
├── Entities/          # 实体 Prefab
├── Scenes/            # 场景
├── UI/UIForms/        # UI Prefab
├── Scripts/
│   ├── Base/          # GameEntry
│   ├── Procedure/
│   │   ├── InitProcedure/   # 启动链（一般少改）
│   │   └── CustomProcedure/ # 玩法流程（主要扩展点）
│   ├── UI/
│   ├── Entity/
│   ├── DataTable/     # 生成代码 DR*.cs
│   ├── Definition/    # Constant、数据结构
│   ├── Control/
│   ├── Platform/
│   ├── Network/
│   ├── ProgressObserver/
│   ├── Utility/           # 见 Utility/README.md，按子目录分类
│   │   ├── Asset/         # AssetUtility
│   │   ├── Battle/        # 战斗域工具
│   │   ├── Common/        # CommonUtility
│   │   ├── Json/          # LitJSON 引导
│   │   ├── Mesh/          # RuntimeMeshCombine
│   │   ├── Transform/     # TransformAxisAimAt
│   │   ├── Web/           # WebUtility
│   │   └── ThirdParty/    # 第三方遗留（如 Invector/vExtensions）
│   └── Editor/
```

### 新文件放置规则

- 玩法流程 → `Procedure/CustomProcedure/`
- UI 脚本 → `UI/`，文件名与 Prefab 逻辑类一致
- 实体脚本 → `Entity/` 或 `Entity/{分类}/`
- 实体 Data → 与 Entity 同目录或 `Entity/{分类}/`
- 网络包 → `Network/Packet/`，Handler → `Network/PacketHandler/`
- 工具类 → `Utility/{域}/`，禁止在 `Utility/` 根目录新增 `.cs`；第三方/vendor 放 `Utility/ThirdParty/`

## 3. 命名规范

| 类型 | 规则 | 示例 |
|------|------|------|
| 流程类 | `Procedure` + 名称 | `ProcedureMenu`, `ProcedureBattle` |
| UI 类 | 与 Prefab 同名 | `MenuForm`, `DialogForm` |
| 实体类 | 语义名 | `BattleCattyEntity` |
| EntityData | 实体名 + `Data` | `BattleCattyData` |
| 配置行 | `DR` + 表名 | `DRUIForm` |
| 枚举（表生成） | 表名 + `Enum` | `UIFormEnum`, `SceneEnum` |
| 枚举（流程） | `ProcedureEnum` | 手写于 `ProcedureBase.cs` |
| 扩展类 | 模块 + `Extension` | `UIExtension` |
| 常量 | `Constant.{分类}` | `Constant.EntityGroup.Default` |

字段：UGF 生成代码用 `m_` 前缀；新写业务代码可跟现有文件风格（项目内混用 `m_` 与属性封装）。

## 4. GameEntry 扩展

新增 `GameFrameworkComponent` 子类时：

1. 组件挂到 `GameEntryScene` 的 GameFramework 物体上
2. 在 `GameEntry.Custom.cs` 增加静态属性
3. 在 `InitCustomComponents()` 中 `GetComponent`

不要改 `GameEntry.Builtin.cs` 除非 UGF 升级需要同步模板。

## 5. 配置表约定

- 源表：`Assets/MainGame/DataTables/Excel/`（工作表名 = 输出 txt 名）
- 文本：`Assets/MainGame/DataTables/Text/{TableName}.txt`
- 生成 C#：`Assets/MainGame/Scripts/DataTable/DR{TableName}.cs`
- 生成二进制：`Assets/MainGame/DataTables/Bytes/{TableName}.bytes`
- txt 第一列为 `#` 注释行；数据行 Tab 分隔
- 列名 PascalCase，匹配 `[A-Z][A-Za-z0-9_]*`

修改流程：**Excel 或 txt → 菜单 B 生成 → 提交 txt + bytes + DR*.cs**。

## 6. Procedure 约定

- 启动链（Launch / Preload / ChangeScene）少改
- 玩法流程继承 `ProcedureFlowBase`
- 进入流程时如需 ProgressObserver：`GameEntry.ProgressObserver?.OnProcedureEnter(typeof(...))`
- 离开流程对称调用 `OnProcedureLeave`
- 打开上一流程传入的 UI：`OpenUIFromData(procedureOwner)`
- 默认菜单示例：`ProcedureMenu` 在 `OnEnter` 打开 `UIFormEnum.主界面`，在 `OnLeave` 关闭

## 7. UI 约定

- 所有 UI 逻辑继承 `UGuiForm`；脚本名 = Prefab 名 = `UIForm.txt` 的 AssetName
- 打开：`GameEntry.UI.OpenUIForm(UIFormEnum.Xxx, userData)`
- 不要绕过 DataTable 直接用字符串路径 OpenUIForm（除非 Editor 工具）
- 打包/热更：在 Resource Editor 中分配到资源组（如 `ui`）并 Save；**禁止手改** `ResourceCollection.xml`
- 弹窗：`DialogForm` + `UIExtension.OpenDialog` — **当前表中无 Dialog 行，`OpenDialog` 为 stub**；恢复时写入 `UIForm.txt` → 菜单 B → 改回 `OpenUIForm`
- `SettingForm` 同理：有资源、未入表时不可经 `UIFormEnum` 打开
- 多语言：表 `UITranslation` + `GameEntry.Localization`

## 8. Entity 约定

- `EntityData` 从 `ReferencePool` 取用，用完 `ReleaseData`
- `TypeId` 对应 `DREntity.Id`，不是运行时 `Entity.Id`
- 实体组使用 `Constant.EntityGroup` 常量
- 优先级使用 `Constant.AssetPriority`

## 9. 输入约定

- 配置：`Assets/MainGame/Configs/GameControls.inputactions`（`AssetUtility.GetInputActionsAsset`）；改完须 Generate C# Class
- 动作引用：`GameEntry.Control.Controls.Gameplay.*`（官方生成类）
- 玩法只通过 `GameEntry.Control`（`GetAction*` / `GetMoveVector` / `GetLookVector`）；禁止直接读旧 Input API
- 设备开关：`enableKeyboardMouse` / `enableJoystick` / `enableTouchScreen`（bindingMask）
- 分层说明：`Scripts/Control/README.md`

## 10. 网络约定

- 包 Id 全局唯一
- CS 包继承 `CSPacketBase`，SC 包继承 `SCPacketBase`
- SC 包必须配对 `PacketHandlerBase` 子类，且 `Id` 一致
- 使用 `[ProtoContract]` / `[ProtoMember]` 标注字段

## 11. 注释与文档

- 公共 API 写 `<summary>`；非显而易见逻辑写简短注释
- 跨模块约定变更时同步更新 `docs/` 与 `AGENTS.md`
- 不要为自解释代码写废话注释

## 12. 禁止事项

- 修改 `Assets/UnityGameFramework/` 源码
- 手改带「此文件由工具自动生成」的 DR*.cs
- 手改 `Configs/ResourceCollection.xml`（由 Resource Editor Save 生成）
- 业务中 `SceneManager.LoadScene`
- 硬编码 `Assets/MainGame/...` 路径（用 `AssetUtility`）
- 在未注册的情况下新增 Procedure（switch + Inspector）
- 新增 DataTable 却不加入 `ProcedurePreload.DataTableNames`

## 13. Git 与 AI 配置

- 项目文档：`AGENTS.md`、`docs/`、`.cursor/rules/` 纳入版本控制
- 本地 IDE 私有配置仍可 gitignore
