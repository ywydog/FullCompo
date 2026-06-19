# 首次启动体验与面板窗口行为设计

## 1. 问题描述

当前版本首次启动时会出现一个标题为“Window”的空窗口，组件面板没有正确显示在桌面上。用户期望的交互参考 ClassIsland：

- 首次启动无主窗口，直接显示桌面组件面板
- 首次运行弹出欢迎/快速设置窗口
- 面板应为透明、无边框、置顶的悬浮窗
- 托盘图标始终可用

## 2. 目标

1. 消除首次启动时的空主窗口
2. 让面板窗口正确显示在桌面右上角
3. 首次运行时展示欢迎窗口
4. 非编辑模式下支持鼠标穿透（可选，默认关闭）
5. 保持现有托盘图标和设置窗口功能

## 3. 方案

### 3.1 主窗口处理

Avalonia 桌面生命周期需要一个 `MainWindow`，但将其设为不可见占位窗口：

- `Width = 1`, `Height = 1`
- `IsVisible = false`
- `ShowInTaskbar = false`
- `WindowState = Minimized`
- 不调用 `Show()` 或延迟到必要时再显示

真正的 UI 由 `PanelWindow` 和 `AppSettingsWindow` 承担。

### 3.2 面板窗口样式

`PanelWindow` 需要以下属性（在 axaml 中设置）：

- `WindowBorderBrush="Transparent"`
- `ExtendClientAreaToDecorationsHint="True"`
- `ExtendClientAreaChromeHints="NoChrome"`
- `Background="Transparent"`
- `Topmost="True"`
- `ShowInTaskbar="False"`
- `CanResize="False"`
- `SizeToContent="WidthAndHeight"`

面板内部容器使用圆角、半透明背景，依赖主题服务设置 `Background` 和 `Opacity`。

### 3.3 首次运行检测与欢迎窗口

在 `ConfigService.Load()` 之后检查：

```csharp
bool isFirstRun = !File.Exists(settingsPath) && !File.Exists(panelsPath);
```

首次运行时：

1. 先创建默认面板并显示
2. 再弹出 `WelcomeWindow`（非模态），提供：
   - 欢迎使用标题
   - 简单说明（桌面组件、右键编辑、托盘设置）
   - “开始体验”按钮，关闭窗口
   - （可选）开机自启复选框

### 3.4 面板位置计算

`UpdatePosition()` 在窗口 `Opened` 事件后调用，确保 `Screens` 已初始化。默认停靠模式 `TopRightCorner`：

```
X = screen.Width - panel.Width - marginRight
Y = marginTop
```

窗口大小按网格计算：

```
Width  = Columns * CellWidth  + (Columns - 1) * Spacing + MarginLeft + MarginRight
Height = MaxRow * CellHeight + (MaxRow - 1) * Spacing + MarginTop + MarginBottom
```

### 3.5 编辑模式与运行时模式

- 运行时：`PanelBorder.Opacity = theme.Opacity`，无边框/拖拽提示
- 编辑模式：`Opacity = 1.0`，显示网格辅助线和组件边框，支持右键菜单

### 3.6 启动顺序

```
Program.Main
  └── Build Host
  └── ConfigService.Load()          // 加载/生成默认配置
  └── Build AvaloniaApp
        └── OnFrameworkInitializationCompleted
              ├── RegisterBuiltinWidgets
              ├── ApplyTheme
              ├── PanelService.CreateOrUpdatePanels()  // 创建并显示面板
              ├── SetupTrayIcon
              └── if firstRun: ShowWelcomeWindow()
```

## 4. 数据变更

- `AppSettings` 新增 `bool IsFirstRun { get; set; } = true;`，首次启动后设为 `false` 并保存。

## 5. 错误处理

- 面板位置计算失败时回退到 `(0,0)` 并记录日志
- 主题应用失败不影响面板显示
- 欢迎窗口异常不阻塞主程序

## 6. 验收标准

- [ ] 首次启动不再出现“Window”空窗口
- [ ] 桌面右上角出现默认组件面板（日期、天气、时钟）
- [ ] 首次启动弹出欢迎窗口
- [ ] 关闭欢迎窗口后面板保留
- [ ] 托盘图标右键可打开设置和退出
- [ ] 非首次启动直接显示面板，不弹欢迎窗口
