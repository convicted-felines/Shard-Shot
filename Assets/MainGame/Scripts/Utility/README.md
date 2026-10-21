# Utility 目录说明

按职责分子目录，避免根目录堆杂项。新增工具请放入对应子目录，命名空间仍为 `MainGame`（第三方遗留代码除外）。

| 子目录 | 类型 | 说明 |
|--------|------|------|
| `Asset/` | 静态类 | `AssetUtility` — 资源路径唯一入口 |
| `Battle/` | 静态类 | 战斗域纯函数占位（如 `BattleUtility`）；有战斗逻辑时在此扩展 |
| `Common/` | 静态类 | `CommonUtility` — 实体 Id、配置解析、Setting 类型转换 |
| `Json/` | 内部类 | LitJSON 辅助与 Editor 下 JsonHelper 引导 |
| `Mesh/` | MonoBehaviour | `RuntimeMeshCombine` — 运行时合并子 Mesh |
| `Transform/` | MonoBehaviour | `TransformAxisAimAt` — 局部轴瞄准目标 |
| `Web/` | 静态类 | URL 编解码 |
| `ThirdParty/Invector/` | 静态类 | Invector 遗留扩展（`Invector` 命名空间），镜头避障等 |

## 放置规则

- 资源路径 → `Asset/AssetUtility.cs`，禁止业务硬编码 `Assets/MainGame/...`
- 跨模块通用小工具 → `Common/`
- 单域业务工具 → 对应域目录（如 `Battle/`）
- 第三方/vendor 代码 → `ThirdParty/{Vendor}/`，保留原命名空间并注明来源
- 不要在 `Utility/` 根目录新增 `.cs` 文件

## 调用方

| 类 | 主要调用方 |
|----|-----------|
| `AssetUtility` | ProcedurePreload、UIExtension、EntityExtension、SoundExtension |
| `CommonUtility` | SettingExtension、ProgressObserverComponent |
| `GameFrameworkJsonHelperBootstrap` | SettingExtension（Editor 持久数据工具） |
| `RuntimeMeshCombine` | Prefab / 场景组件 |
| `TransformAxisAimAt` | 实体 Prefab 关节瞄准 |
| `vExtensions` | 第三人称镜头（Invector 兼容） |
