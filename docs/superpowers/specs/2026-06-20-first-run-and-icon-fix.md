# 首次启动与托盘图标修复设计

## 问题

1. **托盘图标空白 / Logo 不显示**：代码和 XAML 中使用 `avares://FullCompo.App/Assets/logo.png`，但项目 `<AssemblyName>` 是 `FullCompo`，Avalonia 资源 URI 不匹配，导致资源加载失败。
2. **首次启动流程不符合 ClassIsland 风格**：当前首次启动会提前创建托盘图标，且关闭欢迎窗口未完成设置时仍会创建面板，留下没有可见窗口的后台进程。

## 目标

- 修复 Logo 资源 URI，使托盘图标、欢迎窗口、设置页、关于页都能正确显示图标。
- 首次启动流程改为：只显示欢迎窗口 → 用户点击“完成”后保存配置 → 创建托盘图标 → 创建面板。
- 用户在欢迎窗口未点击“完成”就关闭时，直接退出应用。
- 面板窗口在真正打开后再计算屏幕位置，避免构造函数中屏幕信息未就绪导致位置错误。

## 方案

1. 将所有 `avares://FullCompo.App/Assets/logo.png` 替换为 `avares://FullCompo/Assets/logo.png`。
2. `App.axaml.cs` 中：
   - `SetupTrayIcon` 先加载 `Icon`，加载成功后再设置 `_trayIcon.IsVisible = true`。
   - 首次启动时先不创建托盘图标，等 `WelcomeWindow.Completed` 触发后再根据 `ShowTrayIcon` 创建。
   - `WelcomeWindow.Closed` 在未完成时调用 `Shutdown()` 退出应用。
3. `PanelWindow.axaml.cs` 中在 `Opened` 事件里再调用一次 `UpdatePosition()`。

## 验证

- 本地 `dotnet build` 通过。
- 在 Xvfb 中运行能看到“欢迎使用 FullCompo”窗口。
- CI 构建产物在 Windows 上首次启动时能看到托盘图标与面板。
