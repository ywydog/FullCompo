# 欢迎页恢复与默认小组件布局设计

日期：2026-06-19

## 目标

1. 恢复并修复首次运行欢迎页（外观选择失效、字体看不清、下一步按钮消失）。
2. 重新设计默认桌面布局：两个短条在左、一个方形在右。
3. 实现三种基础组件尺寸预设，尺寸集中在一个文件，方便用户自行修改。
4. 时钟组件改为现实风格的模拟指针表盘。
5. 文字/内容在各尺寸内自适应铺满。

## 默认布局

```
┌─────────────┬─────────────┐
│ 日期短条    │             │
│ 周五 06/30  │   模拟时钟   │
├─────────────┤   方形       │
│ 天气短条    │             │
│ ☁ 27°C     │             │
└─────────────┴─────────────┘
```

面板总尺寸（可改）：
- 短条：宽 160，高 70
- 方形：宽 160，高 160
- 默认停靠：屏幕右上角（TopRightCorner）

## 尺寸预设

新增 `FullCompo.Core/Models/WidgetSizePresets.cs`，集中管理：

| Id | 名称 | 宽 | 高 | Columns | Rows |
|---|---|---|---|---|---|
| short-bar | 短条 | 160 | 70 | 2 | 1 |
| square | 方形 | 160 | 160 | 2 | 2 |
| large-square | 大方形 | 220 | 220 | 3 | 3 |

用户之后想改大小，直接改这个文件的 `Width` / `Height` 数值即可。

## 欢迎页

重新创建 `FullCompo.App/Views/WelcomeWindow.axaml`：

- 4 步向导：欢迎 → 外观 → 快捷设置 → 完成。
- 背景浅灰 `#F5F5F5`，标题深黑 `#1A1A1A`，正文 `#333333`，蓝色强调 `#0078D4`。
- 外观页使用单选按钮（浅色 / 深色 / 毛玻璃），避免下拉框绑定失效。
- 底部导航按钮（上一步 / 下一步）始终可见；最后一步“下一步”变为“完成”。
- 窗口 `Topmost="True"`、`WindowStartupLocation="CenterScreen"`、`CanResize="False"`。

## 内置组件改造

### 日期短条

- 内容：`周五 06/30`
- 使用 `Viewbox` 包裹 `TextBlock`，文字随短条高度自适应。
- 文字颜色使用主题前景色 `ThemeForegroundColor`。
- 背景透明，依赖面板主题背景。

### 天气短条

- 内容：`☁ 27°C`
- 同样使用 `Viewbox` 自适应。
- 文字颜色使用主题前景色。

### 时钟方形

- 改为模拟指针表盘：
  - 外圈圆环。
  - 数字 1–12 均匀分布。
  - 时针、分针、秒针，每秒刷新。
  - 表盘整体随方形尺寸缩放。
- 使用 `Canvas` + `Ellipse`/`Line`/`TextBlock` 绘制。
- 不显示数字时间，只显示指针表盘。

## 文件变更

- 新增：`FullCompo.App/Views/WelcomeWindow.axaml` + `.axaml.cs`
- 新增：`FullCompo.Core/Models/WidgetSizePresets.cs`
- 修改：`FullCompo.App/App.axaml.cs`（首次运行打开欢迎页）
- 修改：`FullCompo.Core/Services/ConfigService.cs`（默认布局）
- 修改：`FullCompo.Widgets.Builtin/DateWidget.cs`
- 修改：`FullCompo.Widgets.Builtin/WeatherWidget.cs`
- 修改：`FullCompo.Widgets.Builtin/ClockWidget.cs`

## 鼠标穿透

面板必须支持真正的鼠标穿透（置顶时也能点击到桌面/其他窗口）：

- Windows 平台：通过 `SetWindowLong` + `WS_EX_TRANSPARENT` 实现真正的 OS 级穿透。
- 编辑模式下自动关闭穿透，方便拖拽组件。
- 设置项 `AppSettings.ClickThrough` 默认开启。
- 仅在非编辑模式且设置开启时应用穿透。

## 组件间距设置

- 设置项：`AppSettings.WidgetSpacing`（double，范围 0.0 ~ 2.0）。
- 实际像素间距 = `WidgetSpacing × 16`。
- 默认值 `0.5`（即 8px）。
- 在 [AppSettingsWindow.axaml](file:///workspace/FullCompo.App/Views/AppSettingsWindow.axaml) 增加滑块。

## 天气组件（ClassIsland 同款小米天气 API）

- 使用 ClassIsland 同款接口：`https://weatherapi.market.xiaomi.com/wtr-v3/weather/all`。
- 请求参数：
  - `locationKey=weathercn:{城市代码}`
  - `sign=zUFJoAR2ZVrDy1vF3D07`
  - `isGlobal=false`
  - `locale=zh_cn`
  - `days=1`
  - `latitude=0&longitude=0`
- 新增设置项 `AppSettings.WeatherCity`（默认“北京”），在 [AppSettingsWindow.axaml](file:///workspace/FullCompo.App/Views/AppSettingsWindow.axaml) 增加城市输入框。
- 内置常见城市代码映射（北京、上海、广州、深圳、杭州、成都等 30+），找不到时回退北京。
- 天气组件改为显示彩色图标 + 温度，图标先用彩色 Emoji（☀️🌤️☁️🌧️⛈️❄️等），后续可替换为图片资源。
- 每 10 分钟自动刷新一次。

### 文件变更

- 新增：`FullCompo.Core/Abstractions/Services/IWeatherService.cs`
- 新增：`FullCompo.Core/Services/WeatherService.cs`
- 新增：`FullCompo.Core/Models/WeatherData.cs`
- 新增：`FullCompo.Core/Helpers/WeatherCityCodes.cs`
- 修改：`FullCompo.Shared/Models/AppSettings.cs`（增加 WeatherCity）
- 修改：`FullCompo.App/Views/AppSettingsWindow.axaml` + `.axaml.cs`（增加城市设置）
- 修改：`FullCompo.App/Program.cs`（注册 WeatherService 单例）
- 修改：`FullCompo.Widgets.Builtin/WeatherWidget.cs`（调用天气服务显示实时数据）

## 兼容性

- 用户首次运行：弹出欢迎页。
- 删除 `%AppData%\FullCompo\data` 后重新运行，可再次进入首次运行流程。
