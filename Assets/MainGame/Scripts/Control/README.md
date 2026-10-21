# Control 输入模块

玩法只读控制组件，不直接碰旧 Input API。运行时以官方生成的 `GameControls` 为准。

```
Configs/GameControls.inputactions  → Generate C# Class → GameControls.cs
                                         ↓
                              ControlComponent (GameEntry.Control)
                                · bindingMask 设备开关
                                · Block / Enable 总开关
                                · Suppress 手柄 UI 吞输入
                                         ↓
                              玩法 GetAction* / GetMoveVector / GetLookVector
```

## 文件职责

| 文件 | 职责 |
|------|------|
| `Configs/GameControls.inputactions` | 动作与绑定（改完须 Generate C# Class） |
| `GameControls.cs` | Unity 自动生成包装，**勿手改** |
| `ControlComponent.cs` | 总开关、设备开关、玩法查询 API |
| `PointerInputUtility.cs` | UI 指针（CommonButton 等） |
| `InputSystemUiBootstrap.cs` | EventSystem → InputSystemUIInputModule |

## 玩法怎么读输入

```csharp
// 进入可操作玩法时
GameEntry.Control.EnableAllGameControl();

var g = GameEntry.Control.Controls.Gameplay;
Vector2 move = GameEntry.Control.GetMoveVector();

if (GameEntry.Control.GetActionDown(g.Jump))
{
    // ...
}

// 只关手柄
GameEntry.Control.enableJoystick = false;

// 过场 / 菜单
GameEntry.Control.BlockAllGameControl();
```

## 新增动作

见 `docs/ADD_FEATURE.md` §9。

1. 编辑 `Configs/GameControls.inputactions`（绑定挂到对应 Control Scheme）
2. Inspector 上 **Generate C# Class**
3. 玩法用 `Controls.Gameplay.新动作`

## 注意

- 默认 `BlockAllInput = true`，需显式 `EnableAllGameControl` 才会 Enable Gameplay 地图
- 禁止在玩法里直接 `Input.GetAxis`；经 `GameEntry.Control` 查询；UI 点击用 `PointerInputUtility`
- 改 `.inputactions` 后忘记 Generate，运行时仍是嵌入 JSON 的旧绑定
