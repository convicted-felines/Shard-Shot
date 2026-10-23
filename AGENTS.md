# AI Agent 指南

本文件为 Cursor / AI 助手提供项目级上下文。修改代码前请先阅读本文件及 `docs/` 目录。

## 项目概览

- **引擎**：Unity 2022.3 LTS + URP
- **框架**：Unity Game Framework（UGF），位于 `Assets/UnityGameFramework/`
- **业务代码**：`Assets/MainGame/`，命名空间 `MainGame`（Editor 工具为 `MainGame.Editor`）
- **入口场景**：`Assets/MainGame/Scenes/GameEntryScene.unity`

## 目录边界（必须遵守）

| 路径 | 可否修改 | 说明 |
|------|----------|------|
| `Assets/MainGame/` | ✅ 主要工作区 | 流程、UI、实体、配置、业务逻辑 |
| `Assets/UnityGameFramework/` | ❌ 不要改 | 第三方框架，升级时会被覆盖 |
| `Assets/MainGame/Scripts/DataTable/DR*.cs` | ❌ 不要手改 | 由 DataTable 生成器产出，会被覆盖 |
| `Assets/MainGame/DataTables/Bytes/*.bytes` | ❌ 不要手改 | 由生成器产出 |
| `ProjectSettings/` | ⚠️ 谨慎 | 仅在明确要求时修改 |

## 核心架构速查

```
GameEntry (partial)
├── Builtin 组件 → GameEntry.Builtin.cs（UGF 内置）
└── Custom 组件  → GameEntry.Custom.cs（BuiltinData / Control / Platform / ProgressObserver）

启动：ProcedureLaunch → Splash → 资源分支 → Preload → ProcedureChangeScene → ProcedureMenu（打开 MenuForm）
切场景/切流程：仅通过 ProcedureFlowBase → ProcedureChangeScene，禁止 SceneManager.LoadScene
资源路径：统一用 `Utility/Asset/AssetUtility`，禁止硬编码 Assets/ 路径
UI 打开：GameEntry.UI.OpenUIForm(UIFormEnum.Xxx) 或 UIExtension
实体创建：EntityExtension.ShowEntity（见 docs/ADD_FEATURE.md）
输入：`Configs/GameControls.inputactions` → Generate `GameControls` → `GameEntry.Control`（设备开关 / 总开关 / 玩法查询）
打包资源：禁止手改 `Configs/ResourceCollection.xml`；用菜单 `Game Framework/Resource Tools/Resource Editor` 分配资源并 Save（Editor 资源模式可直接读 Assets，不必先进 Collection）
```

详细架构见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)。

## 编码约定摘要

- 命名空间：业务代码统一 `MainGame`，Editor 用 `MainGame.Editor` 或 `MainGame.Editor.DataTableTools`
- 新 Custom 组件：在 `GameEntry.Custom.cs` 注册静态访问器
- 常量：放 `Assets/MainGame/Scripts/Definition/Constant/`
- 扩展方法：按模块放 `*Extension.cs`（如 `UIExtension`、`EntityExtension`）
- 中文枚举名：项目已使用（如 `ProcedureEnum.菜单流程`），新增时保持一致

完整约定见 [docs/CONVENTIONS.md](docs/CONVENTIONS.md)。

## 新增功能入口

| 任务 | 文档章节 |
|------|----------|
| 新增流程 Procedure | [ADD_FEATURE §1](docs/ADD_FEATURE.md#1-新增流程-procedure) |
| 新增 UI 界面 | [ADD_FEATURE §2](docs/ADD_FEATURE.md#2-新增-ui-界面) |
| 新增实体 Entity | [ADD_FEATURE §3](docs/ADD_FEATURE.md#3-新增实体-entity) |
| 新增网络协议 | [ADD_FEATURE §4](docs/ADD_FEATURE.md#4-新增网络协议) |
| 新增配置表 | [ADD_FEATURE §5](docs/ADD_FEATURE.md#5-新增配置表) |
| 新增进度观察器 | [ADD_FEATURE §6](docs/ADD_FEATURE.md#6-新增进度观察器-progressobserver) |
| 新增平台能力 | [ADD_FEATURE §7](docs/ADD_FEATURE.md#7-新增平台能力) |

## 常见错误（避免）

1. **直接 LoadScene** — 必须走 `ProcedureFlowBase.StartGame` + `ProcedureChangeScene`
2. **手改 DR*.cs** — 改 `DataTables/Text/*.txt` 或 Excel，再执行 `DataTables/B.生成数据表代码和byte文件`
3. **UI Prefab 路径不一致** — Prefab 名须与表内 `AssetName` 对应，路径规则见 `AssetUtility.GetUIFormAsset`
4. **Procedure 未注册** — 新流程类须加入 `ProcedureBase.ChangeToTargetProcedure` 的 switch，并在 Inspector Available Procedures 勾选
5. **改 UnityGameFramework** — 业务逻辑应扩展 MainGame，不要改框架源码
6. **新增 DataTable 未加入 Preload** — 新表名须写入 `ProcedurePreload.DataTableNames`
7. **手改 ResourceCollection.xml** — 该文件由 Resource Editor 保存生成；应在工具中分配资源后 Save，不要直接改 XML
8. **脚本与 Prefab 脱节** — Prefab 名、脚本名、`UIForm.txt` 的 AssetName 三者必须一致（如 `MenuForm`）

## 编辑器菜单

| 菜单 | 用途 |
|------|------|
| `DataTables/A.Excel导出数据表文本` | Excel → Text |
| `DataTables/B.生成数据表代码和byte文件` | Text → DR*.cs + Bytes |
| `DataTables/C.持久数据编辑工具` | 本地 Setting 调试 |
| `Game Framework/Resource Tools/Resource Editor` | 分配资源组并生成 `ResourceCollection.xml` |

## 相关文档

- [README.md](README.md) — 模板启动说明与当前能力一览
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — 模块关系与数据流
- [docs/CONVENTIONS.md](docs/CONVENTIONS.md) — 命名、目录、代码风格
- [docs/ADD_FEATURE.md](docs/ADD_FEATURE.md) — 分步 Checklist
- [docs/JUICE_PLAN.md](docs/JUICE_PLAN.md) — 手感细化 3 天计划（Juice / 音效 / 刷怪 / HUD）
- [.cursor/rules/](.cursor/rules/) — Cursor 持久规则
