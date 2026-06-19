# 灵动岛面板设计

## 目标

将 FullCompo 的面板从 Grid 网格布局改造为类似 ClassIsland 的灵动岛横条样式，悬浮在屏幕顶部，组件水平排列，可交互。

## 当前问题

- 面板使用 Grid 网格布局，组件按行列排列，不像"岛"
- 没有阴影效果，面板不够"浮起来"
- 组件各自带独立背景 Border，视觉不统一
- 默认停靠右上角，不够醒目
- 高度不固定，随组件数量变化

## 设计方案

### 1. 面板布局改造

**从 Grid 改为单行水平排列：**

```
┌──────────────────────────────────────────────────┐
│  📅 周三 06/18  │  🔍 搜索...  │  🕐 14:32:05  │  ☀️ 28°C  │
└──────────────────────────────────────────────────┘
              ↑ 屏幕顶部居中悬浮
```

- 使用 `StackPanel Orientation="Horizontal"` 替代 Grid
- 组件从左到右水平排列，间距 8px
- 面板高度固定 80px（宽松模式，给交互组件足够空间）
- 面板宽度 `SizeToContent="WidthAndHeight"` 自适应内容

### 2. 面板窗口属性

```xml
<Window SystemDecorations="None"
        TransparencyLevelHint="Transparent"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        CanResize="False"
        SizeToContent="WidthAndHeight">
```

### 3. 面板 Border 样式

```xml
<Border x:Name="PanelBorder"
        Background="{DynamicResource ThemeBackgroundColor}"
        BorderBrush="{DynamicResource ThemeBorderColor}"
        BorderThickness="1"
        CornerRadius="20"
        Padding="12,8"
        BoxShadow="0 4 12 2 #40000000">
    <StackPanel x:Name="WidgetStack"
                Orientation="Horizontal"
                Spacing="8" />
</Border>
```

关键视觉属性：
- **圆角**: 20px（胶囊形），可通过主题配置
- **阴影**: `0 4 12 2 #40000000`（半透明黑色，让岛浮起来）
- **内边距**: 左右12px，上下8px
- **组件间距**: 8px

### 4. 组件样式改造

组件不再自带背景 Border，融入岛背景：

**之前（ClockWidget）：**
```csharp
return new Border
{
    Background = new SolidColorBrush(Color.Parse("#55FFFFFF")),
    CornerRadius = new CornerRadius(12),
    Padding = new Thickness(12),
    Child = textBlock
};
```

**之后：**
```csharp
return new Border
{
    // 无独立背景，融入岛
    CornerRadius = new CornerRadius(8),
    Padding = new Thickness(12, 4),
    Child = textBlock
};
```

组件字体大小规范：
- 小组件(1x1): 14px
- 中组件(2x2): 16-18px
- 大组件: 20px

### 5. 默认停靠位置

改为屏幕顶部居中，而非右上角：

```csharp
PanelDockMode.TopCenter => new PixelPoint(
    bounds.X + (bounds.Width - (int)totalWidth) / 2,
    bounds.Y + (int)_config.MarginTop)
```

新增 `TopCenter` 停靠模式，作为默认值。

### 6. 不透明度

非编辑模式下，面板背景不透明度可通过主题配置（默认 0.9）。
编辑模式下，不透明度 1.0，显示编辑边框。

### 7. 分体模式（可选，暂不实现）

每个组件独立成"岛"，各自有背景和圆角。后续版本考虑。

## 需要修改的文件

1. **PanelWindow.axaml** — 改为 StackPanel 水平布局
2. **PanelWindow.axaml.cs** — 改为水平排列逻辑，新增 TopCenter 停靠
3. **PanelDockMode.cs** — 新增 TopCenter / BottomCenter 枚举值
4. **PanelConfig.cs** — 调整默认值（DockMode=TopCenter, CellHeight=80）
5. **WidgetHost.cs** — 适配水平布局
6. **ThemeService.cs** — 调整主题圆角和阴影
7. **各 Widget** — 移除独立背景，统一字体大小
8. **ConfigService.cs** — 调整默认面板配置

## 验收标准

- [ ] 面板悬浮在屏幕顶部居中
- [ ] 组件水平排列，间距均匀
- [ ] 面板有圆角和阴影，视觉上像"岛"
- [ ] 组件可交互（搜索框可输入，按钮可点击）
- [ ] 编辑模式正常工作（拖拽、右键菜单）
- [ ] 主题切换正常
