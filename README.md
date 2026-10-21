# GameFramework-26.8

基于速降单车项目裁剪的 Unity GameFramework 模板工程（Unity 2022.3 LTS + URP）。

## 启动流程

```
GameEntryScene（框架加载）
  ProcedureLaunch → Splash → 资源分支 → Preload
    → ProcedureChangeScene 加载 Menu.unity
      → ProcedureMenu 打开 MenuForm（主界面）
```

## 场景

| 场景 | 路径 | 作用 |
|------|------|------|
| 入口 | `Assets/MainGame/Scenes/GameEntryScene.unity` | 放置 `GameFramework`，启动流程 |
| 菜单 | `Assets/MainGame/Scenes/Menu.unity` | 启动完成后的默认场景；由 `ProcedureMenu` 打开主界面 UI |

Build Settings 已加入上述两场景。运行 `GameEntryScene` 即可验证跳转与主菜单 UI。

## 当前模板已有内容

| 模块 | 状态 |
|------|------|
| 启动 / 资源更新链 | 完整（InitProcedure） |
| 菜单流程 + 主界面 | `ProcedureMenu` + `MenuForm`（占位按钮，可扩展） |
| SettingForm / DialogForm | Prefab 与脚本已有，**尚未写入** `UIForm.txt`（`OpenDialog` 暂为 stub） |
| 实体 / 战斗流程 / 业务网络 | 基建就绪，玩法内容待填 |

## 新项目起步

1. 在 `MenuForm` 上扩展按钮逻辑，或新增玩法 `Procedure` + 场景（见 `docs/ADD_FEATURE.md`）
2. 用 Data Table Generator 维护表数据  
3. 需要打包/热更时：在 `Game Framework/Resource Tools/Resource Editor` 中分配资源并 Save（**不要手改** `ResourceCollection.xml`）

## 文档（AI 协作 / 团队约定）

| 文档 | 说明 |
|------|------|
| [AGENTS.md](AGENTS.md) | AI 助手项目地图与禁止事项 |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | 模块架构与数据流 |
| [docs/CONVENTIONS.md](docs/CONVENTIONS.md) | 命名、目录、编码约定 |
| [docs/ADD_FEATURE.md](docs/ADD_FEATURE.md) | 新增 Procedure / UI / Entity 等 Checklist |
| [.cursor/rules/](.cursor/rules/) | Cursor 持久规则 |
