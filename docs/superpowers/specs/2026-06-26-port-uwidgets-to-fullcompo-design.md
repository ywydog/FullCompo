# 将 uWidgets 组件移植到 FullCompo 的设计文档

## 1. 目标

把 [uWidgets](https://github.com/creewick/uWidgets) 的 6 类桌面组件（Clock、Calendar、Notes、Reminders、Weather、Monitor）按照 FullCompo 现有的 `IWidget`/`WidgetBase` 风格重写，全部集成进 `FullCompo.Widgets.Builtin` 项目。

## 2. 范围与阶段

| 阶段 | 内容 | 说明 |
|------|------|------|
| 第一期 | 新增 Calendar、Notes、Reminders、Monitor | 这四个组件在 FullCompo 中不存在，直接新增 |
| 第二期 | 升级 ClockWidget 和 WeatherWidget | 用 uWidgets 的功能增强现有组件，保持原有 widget ID 不变 |
| 第三期 | 设置页、图标、主题适配 | 为每个组件补齐 `CreateSettingsView`，并适配 FullCompo 主题资源 |

## 3. 架构原则

- **不改动** `FullCompo.App`、`FullCompo.Core`、`FullCompo.Shared` 的 DI、面板、配置、主题框架。
- 所有新组件都实现 `FullCompo.Core.Abstractions.IWidget`，继承 `WidgetBase`。
- 在 `FullCompo.Widgets.Builtin` 内按组件建立子目录：
  - `Widgets/Clock/`
  - `Widgets/Calendar/`
  - `Widgets/Notes/`
  - `Widgets/Reminders/`
  - `Widgets/Weather/`
  - `Widgets/Monitor/`
- 公共能力（定时器、天气网络请求、WMI 监控）先以内联或私有 static 方式实现，避免一次重构面过大；稳定后再抽取到 Core/Shared。

## 4. 组件映射

| uWidgets 组件 | FullCompo 目标类 | 主要设置项 |
|---------------|------------------|------------|
| Clock（Analog I/II/III、Digital、World） | `ClockWidget` | `ClockStyle`, `ShowSeconds`, `TimeZoneId` |
| Calendar（Month / Date） | `CalendarWidget` | `ViewMode`, `FirstDayOfWeek` |
| Notes | `NotesWidget` | `Header`, `Content` |
| Reminders | `RemindersWidget` | `Header`, `Items` |
| Weather（Forecast / Temperature / UV / SunriseSunset / Pressure / AirQuality） | `WeatherWidget` | `ViewMode`, `Latitude`, `Longitude`, `TemperatureUnit` |
| Monitor（CPU / RAM / Disk / Network / Battery） | `MonitorWidget` | `MetricType` |

## 5. 数据流

1. `DesktopSurfaceWindow.LoadWidgets` 从 `ConfigService.Panels` 读取面板与组件实例配置。
2. 通过 `IWidgetRegistry.GetWidget(config.WidgetId)` 找到对应组件实现。
3. `WidgetContainer` 封装组件视图，调用 `widget.CreateView(context)` 渲染内容。
4. 组件内部使用 `DispatcherTimer` 定时刷新；Weather、Monitor 等异步数据在后台获取后通过 `Dispatcher.UIThread.Post` 更新界面。
5. 编辑模式下，`widget.CreateSettingsView(settings)` 返回设置控件，用户修改后写入 `WidgetSettings`。
6. 退出编辑模式时 `ConfigService.Save()` 将 `Panels` 持久化到 `panels.json`。

## 6. 错误处理

- 每个 `CreateView` 内部使用 `try/catch`，失败时返回占位文本（如“--”或“加载失败”），避免单个组件崩溃影响整个桌面。
- `DesktopSurfaceWindow` 与 `PanelService` 继续在外层捕获异常，保证桌面壳不退出。

## 7. 依赖与约束

- 目标框架保持 `net8.0`。
- Monitor 组件依赖 Windows WMI（`System.Management`），与 uWidgets 保持一致，非 Windows 平台返回 `null`。
- Weather 组件改用 Open-Meteo API（与 uWidgets 一致），不再使用原 Xiaomi 天气接口；`AppSettings` 中新增/替换经纬度与城市名字段。
- 保留 FullCompo 现有 logo 与应用品牌。

## 8. 验收标准

- 解决方案能编译通过。
- 默认布局能正常加载，不报错。
- 每个新组件至少支持 `small-square`、`medium-hbar`、`medium-square` 三种尺寸。
- 编辑模式下能打开每个组件的设置面板并保存。

## 9. 后续可优化（不在本次范围）

- 抽取公共 `ITimerService`、`IWeatherProvider`、`IMetricProvider` 到 Core。
- 支持组件插件化，允许第三方 DLL 注册 `IWidget`。
- 引入 Avalonia 动画与更完善的无障碍支持。
