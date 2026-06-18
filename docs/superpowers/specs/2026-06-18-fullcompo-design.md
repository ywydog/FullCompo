# 全面组件（FullCompo）设计文档

## 1. 项目概述

**全面组件（FullCompo）** 是一款基于 .NET 8/9 + Avalonia 开发的跨平台多功能桌面组件软件。用户可以通过组合各种组件，在桌面上搭建个性化的信息展示与效率工具面板。

灵感参考 ClassIsland 的贴边悬浮面板体验，但扩展为更通用的组件平台，支持信息展示、效率工具、自定义内容等多种组件类型。

## 2. 核心定位

- **目标平台**：Windows 10/11、Linux、macOS
- **形态**：屏幕边缘/角落的悬浮组件面板，平时锁定，进入编辑模式后可调整
- **扩展性**：内置常用组件 + C# 插件 + Lua 脚本扩展
- **配置**：本地 JSON 配置文件 + 主题系统

## 3. 技术栈

| 技术 | 用途 |
|------|------|
| .NET 8/9 | 运行时与开发框架 |
| C# | 主要编程语言 |
| Avalonia UI | 跨平台 UI 框架 |
| FluentAvalonia | Windows 11 风格主题 |
| Microsoft.Extensions.Hosting | IoC 依赖注入容器 |
| ReactiveUI | 响应式绑定（可选） |
| MoonSharp | Lua 脚本宿主 |
| System.Text.Json | 配置序列化 |

## 4. 项目结构

```
FullCompo/
├── FullCompo.App/              # 主程序入口、宿主服务、系统托盘、设置窗口
├── FullCompo.Core/             # 核心抽象：IWidget、IPanelService、主题基类
├── FullCompo.Shared/           # 配置模型、枚举、常量、JSON 辅助
├── FullCompo.Widgets.Builtin/  # 内置组件实现
├── FullCompo.PluginSdk/        # 插件开发 SDK（后续打包为 NuGet）
└── FullCompo.Scripting/        # Lua 脚本宿主
```

## 5. 核心概念

### 5.1 组件（Widget）

每个组件实现 `IWidget` 接口：

```csharp
public interface IWidget
{
    string Id { get; }              // 唯一标识，如 "builtin.clock"
    string Name { get; }            // 显示名称
    string Description { get; }     // 简介
    IconSource Icon { get; }        // 组件图标
    IEnumerable<WidgetSize> SupportedSizes { get; }  // 支持的尺寸模板

    Control CreateView(WidgetContext context);              // 创建组件视图
    Control? CreateSettingsView(WidgetSettings settings);   // 创建设置视图（可选）
    WidgetSettings CreateDefaultSettings();                 // 默认配置
    void OnActivated(WidgetContext context);                // 组件激活
    void OnDeactivated();                                   // 组件停用
}
```

`WidgetContext` 包含实例 ID、所属面板、当前尺寸、全局 `IServiceProvider`、专用 `ILogger` 和配置读写接口。

### 5.2 组件尺寸分类

组件按网格单元格占用尺寸分为三类：

| 类型 | 尺寸模板 | 典型用途 |
|------|----------|----------|
| 小类型 | 1×1, 2×1, 1×2, 圆形 | 日期、天气小条、系统监控、快捷图标 |
| 中类型 | 2×1, 1×2, 3×1 | 便签、搜索框、天气详情、快捷启动组 |
| 大类型 | 2×2, 3×2, 2×3 | 课表/日程、待办清单、大文本/图片 |

每个 `IWidget` 注册时声明自己支持的尺寸模板，用户添加时选择模板。

### 5.3 面板（Panel）

面板是 Avalonia 窗口，作为组件的容器：

- 每个面板定义列数和单元格尺寸
- 组件按网格定位，声明 `RowSpan` 和 `ColumnSpan`
- 支持停靠模式：浮动、顶部、底部、左侧、右侧、四角
- 默认首次启动在屏幕右上角

```csharp
public class PanelConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public PanelDockMode DockMode { get; set; }
    public Thickness Margin { get; set; }
    public int Columns { get; set; }
    public double CellWidth { get; set; }
    public double CellHeight { get; set; }
    public double Spacing { get; set; }
    public List<WidgetInstanceConfig> Widgets { get; set; }
}
```

### 5.4 组件实例（WidgetInstance）

```csharp
public class WidgetInstanceConfig
{
    public string Id { get; set; }          // 实例唯一 ID
    public string WidgetId { get; set; }    // 组件类型 ID
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; }
    public int ColumnSpan { get; set; }
    public WidgetSettings Settings { get; set; }
}
```

## 6. 默认布局

首次启动时自动创建默认面板：

- **位置**：屏幕右上角（默认边距 Right=16, Top=48）
- **网格**：4 列，行高自适应
- **初始组件**：
  - 日期组件（小横条，1×1）
  - 天气组件（小横条，1×1）
  - 时钟组件（中等方块，2×2，含模拟时钟 + 数字时间）

## 7. 编辑模式与运行时模式

### 7.1 运行时模式

- 面板置顶显示
- 组件位置锁定，不可拖动
- 组件内容可交互（如点击天气查看详情、点击快捷启动打开应用）
- 支持配置鼠标是否穿透（click-through）

### 7.2 编辑模式

通过右键菜单、系统托盘或快捷键进入：

- 显示网格辅助线
- 组件显示边框和拖拽手柄
- 支持拖动组件、调整尺寸、添加/删除组件
- 支持调整面板属性
- 退出编辑模式时自动保存配置

## 8. 插件与脚本扩展

### 8.1 C# 插件

```csharp
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    void Initialize(IPluginContext context);
    void Shutdown();
}

public interface IWidgetProvider
{
    IEnumerable<IWidget> GetWidgets();
}
```

- 插件是独立的 .NET 类库，引用 `FullCompo.PluginSdk`
- 放在 `plugins/` 目录，应用启动时扫描加载
- 使用 `AssemblyLoadContext` 隔离
- 插件可实现 `IWidgetProvider` 注册新组件

### 8.2 Lua 脚本

- 放在 `scripts/widgets/` 目录
- 由 MoonSharp 执行
- 适合简单组件（倒计时、自定义文本、数据展示）
- 可调用宿主提供的 API

```lua
widget = {
    id = "script.example.countdown",
    name = "高考倒计时",
    sizes = { "small", "medium" },
    updateInterval = 1000,
    onUpdate = function(ctx)
        -- 返回显示文本
    end
}
```

## 9. 配置持久化与主题

### 9.1 配置目录

```
%AppData%/FullCompo/
├── settings.json        # 应用级设置
├── panels.json          # 面板和组件实例布局
├── widgets/             # 组件专属数据
└── plugins/             # 插件目录
```

- 使用 `System.Text.Json` 序列化
- 配置变更时自动保存
- 支持导入/导出整套配置

### 9.2 主题系统

```csharp
public class ThemeConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public Color AccentColor { get; set; }
    public double CornerRadius { get; set; }
    public double BorderThickness { get; set; }
    public Color BorderColor { get; set; }
    public string? FontFamily { get; set; }
    public double FontSizeScale { get; set; }
}
```

- 内置多套主题（浅色、深色、半透明、毛玻璃）
- 支持自定义主题
- 主题文件存为 `themes/*.json`

### 9.3 应用级设置

- 开机自启
- 语言
- 默认主题
- 是否显示系统托盘图标
- 是否允许组件穿透点击
- 编辑模式快捷键

## 10. 数据流

```
[配置加载] → [创建面板] → [实例化组件] → [激活组件] → [渲染视图]
     ↑                                                      |
     └──────────────── [退出编辑模式保存] ← [用户编辑布局] ──┘
```

## 11. 核心服务

| 服务 | 职责 |
|------|------|
| `IConfigService` | 读写 JSON 配置 |
| `IPanelService` | 管理所有面板窗口 |
| `IWidgetRegistry` | 注册/发现所有组件 |
| `IPluginService` | 加载/卸载插件 |
| `IScriptingService` | 执行 Lua 脚本 |
| `IThemeService` | 加载/切换主题 |
| `ITrayService` | 系统托盘 |

## 12. 错误处理

- **单个组件异常**：捕获异常，显示占位错误视图，不影响其它组件
- **插件加载失败**：记录日志，跳过该插件，继续启动
- **配置损坏**：尝试读取备份配置，若无备份则恢复默认布局
- **启动崩溃**：提供安全模式启动参数，禁用插件和脚本
- **全局异常**：接入 `AppDomain.UnhandledException` 和 `TaskScheduler.UnobservedTaskException`

## 13. MVP 范围

### 第一阶段（必须实现）

1. 项目骨架搭建
2. 宿主与 DI 容器
3. 面板窗口与网格布局
4. 编辑模式（拖拽、调整尺寸、增删组件）
5. 配置持久化（JSON）
6. 系统托盘
7. 主题系统（至少 2 套）
8. 6 个内置组件：时钟/日期、天气、便签/待办、快捷启动、搜索框、自定义文本/图片

### 第二阶段

- C# 插件加载机制
- Lua 脚本宿主
- 插件 SDK NuGet 包
- 更多停靠模式
- 多语言支持

### 第三阶段

- 组件市场/仓库
- 高级联动规则
- 云端同步配置
