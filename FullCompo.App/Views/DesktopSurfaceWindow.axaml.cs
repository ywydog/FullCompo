using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FullCompo.App.Helpers;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FullCompo.App.Views;

public partial class DesktopSurfaceWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly IWidgetRegistry _widgetRegistry;
    private readonly IThemeService _themeService;
    private readonly ILogger<DesktopSurfaceWindow> _logger;
    private readonly List<WidgetContainer> _widgetContainers = new();

    private bool _isEditMode;
    private WidgetContainer? _selectedContainer;
    private bool _isDragging;

    private const double GridSize = 80.0;
    private const double SnapThreshold = 20.0;

    public bool IsEditMode => _isEditMode;
    public event EventHandler<bool>? EditModeChanged;

    // Windows mouse click-through support
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public DesktopSurfaceWindow(IServiceProvider services)
    {
        _services = services;
        _configService = services.GetRequiredService<IConfigService>();
        _widgetRegistry = services.GetRequiredService<IWidgetRegistry>();
        _themeService = services.GetRequiredService<IThemeService>();
        _logger = services.GetRequiredService<ILogger<DesktopSurfaceWindow>>();

        InitializeComponent();
        SetupWindow();
        SetupToolbar();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupWindow()
    {
        Opened += (_, _) =>
        {
            try
            {
                ApplyDockLayout();
                LoadWidgets();
                UpdateClickThrough();
                if (_isEditMode) EnterEditMode();
            }
            catch (Exception ex)
            {
                AppLog.WriteException("DesktopSurfaceWindow.Opened", ex);
            }
        };

        // Click on empty canvas to deselect
        try
        {
            var widgetsCanvas = this.FindControl<Canvas>("WidgetsCanvas");
            if (widgetsCanvas != null)
            {
                widgetsCanvas.PointerPressed += OnCanvasPointerPressed;
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteException("DesktopSurfaceWindow.SetupWindow canvas", ex);
        }
    }

    private void ApplyDockLayout()
    {
        var screen = Screens.Primary;
        var screenBounds = screen?.Bounds ?? new PixelRect(0, 0, 1920, 1080);
        var dock = _configService.AppSettings.DockPosition;

        // Default top-right panel size (ClassIsland-like capsule)
        const double panelHeight = 140;
        const double panelWidthRatio = 0.38;
        const double sidePanelWidth = 220;

        AppLog.Write($"DesktopSurfaceWindow.ApplyDockLayout: dock={dock}, screen={screenBounds.Width}x{screenBounds.Height}");

        switch (dock)
        {
            case "free":
                WindowState = WindowState.FullScreen;
                Width = screenBounds.Width;
                Height = screenBounds.Height;
                Position = new PixelPoint(screenBounds.X, screenBounds.Y);
                break;

            case "top":
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * 0.8;
                Height = panelHeight;
                Position = new PixelPoint(
                    (int)(screenBounds.X + (screenBounds.Width - Width) / 2),
                    screenBounds.Y + 8);
                break;

            case "top-left":
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * panelWidthRatio;
                Height = panelHeight;
                Position = new PixelPoint(screenBounds.X + 8, screenBounds.Y + 8);
                break;

            case "top-right":
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * panelWidthRatio;
                Height = panelHeight;
                Position = new PixelPoint(
                    (int)(screenBounds.X + screenBounds.Width - Width - 8),
                    screenBounds.Y + 8);
                break;

            case "bottom":
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * 0.8;
                Height = panelHeight;
                Position = new PixelPoint(
                    (int)(screenBounds.X + (screenBounds.Width - Width) / 2),
                    (int)(screenBounds.Y + screenBounds.Height - Height - 8));
                break;

            case "bottom-left":
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * panelWidthRatio;
                Height = panelHeight;
                Position = new PixelPoint(
                    screenBounds.X + 8,
                    (int)(screenBounds.Y + screenBounds.Height - Height - 8));
                break;

            case "bottom-right":
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * panelWidthRatio;
                Height = panelHeight;
                Position = new PixelPoint(
                    (int)(screenBounds.X + screenBounds.Width - Width - 8),
                    (int)(screenBounds.Y + screenBounds.Height - Height - 8));
                break;

            case "left":
                WindowState = WindowState.Normal;
                Width = sidePanelWidth;
                Height = screenBounds.Height * 0.8;
                Position = new PixelPoint(
                    screenBounds.X + 8,
                    (int)(screenBounds.Y + (screenBounds.Height - Height) / 2));
                break;

            case "right":
                WindowState = WindowState.Normal;
                Width = sidePanelWidth;
                Height = screenBounds.Height * 0.8;
                Position = new PixelPoint(
                    (int)(screenBounds.X + screenBounds.Width - Width - 8),
                    (int)(screenBounds.Y + (screenBounds.Height - Height) / 2));
                break;

            default:
                WindowState = WindowState.Normal;
                Width = screenBounds.Width * panelWidthRatio;
                Height = panelHeight;
                Position = new PixelPoint(
                    (int)(screenBounds.X + screenBounds.Width - Width - 8),
                    screenBounds.Y + 8);
                break;
        }

        AppLog.Write($"DesktopSurfaceWindow layout: {Position}, {Width}x{Height}");
    }

    private void UpdateClickThrough()
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return;

            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            var wantClickThrough = _configService.AppSettings.ClickThrough && !_isEditMode;
            var exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            var newExStyle = wantClickThrough
                ? exStyle | WS_EX_TRANSPARENT
                : exStyle & ~WS_EX_TRANSPARENT;

            if (exStyle != newExStyle)
            {
                SetWindowLong(handle, GWL_EXSTYLE, newExStyle);
                AppLog.Write($"DesktopSurfaceWindow click-through: {wantClickThrough}");
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteException("DesktopSurfaceWindow.UpdateClickThrough", ex);
        }
    }

    private void SetupToolbar()
    {
        var btnReset = this.FindControl<Button>("BtnReset");
        var btnAppearance = this.FindControl<Button>("BtnAppearance");
        var btnAdd = this.FindControl<Button>("BtnAdd");
        var btnClear = this.FindControl<Button>("BtnClear");
        var btnUndo = this.FindControl<Button>("BtnUndo");
        var btnRedo = this.FindControl<Button>("BtnRedo");
        var btnDone = this.FindControl<Button>("BtnDone");

        if (btnReset != null) btnReset.Click += (_, _) => ResetLayout();
        if (btnAppearance != null) btnAppearance.Click += (_, _) => OpenAppearance();
        if (btnAdd != null) btnAdd.Click += (_, _) => ShowSizeLibrary();
        if (btnClear != null) btnClear.Click += (_, _) => ClearAllWidgets();
        if (btnUndo != null) btnUndo.Click += (_, _) => Undo();
        if (btnRedo != null) btnRedo.Click += (_, _) => Redo();
        if (btnDone != null) btnDone.Click += (_, _) => ExitEditMode();
    }

    public void LoadWidgets()
    {
        try
        {
            var canvas = this.FindControl<Canvas>("WidgetsCanvas");
            if (canvas == null) return;

            canvas.Children.Clear();
            _widgetContainers.Clear();

            var screen = Screens.Primary;
            var maxX = screen?.Bounds.Width ?? 1920;
            var maxY = screen?.Bounds.Height ?? 1080;

            foreach (var panel in _configService.Panels)
            {
                foreach (var widgetConfig in panel.Widgets)
                {
                    CreateWidgetContainer(widgetConfig, canvas, maxX, maxY);
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteException("DesktopSurfaceWindow.LoadWidgets", ex);
        }
    }

    private void CreateWidgetContainer(WidgetInstanceConfig config, Canvas canvas, double maxX, double maxY)
    {
        var widget = _widgetRegistry.GetWidget(config.WidgetId);
        if (widget == null)
        {
            _logger.LogWarning("Widget {WidgetId} not found", config.WidgetId);
            return;
        }

        var size = widget.SupportedSizes.FirstOrDefault(s => s.Id == config.SizeId)
            ?? widget.SupportedSizes.First();

        var container = new WidgetContainer(config, widget, size, _services);
        container.SelectionChanged += OnContainerSelectionChanged;
        container.DragStateChanged += OnContainerDragStateChanged;
        container.RequestDelete += OnContainerRequestDelete;

        // 保证默认/上次布局不会超出当前屏幕
        var x = Math.Clamp(config.PosX, 0, Math.Max(0, maxX - size.Width));
        var y = Math.Clamp(config.PosY, 0, Math.Max(0, maxY - size.Height));
        Canvas.SetLeft(container, x);
        Canvas.SetTop(container, y);

        canvas.Children.Add(container);
        _widgetContainers.Add(container);
    }

    private void OnContainerSelectionChanged(object? sender, EventArgs e)
    {
        if (sender is not WidgetContainer container) return;
        SelectContainer(container);
    }

    private void OnContainerDragStateChanged(object? sender, bool isDragging)
    {
        _isDragging = isDragging;
    }

    private void OnContainerRequestDelete(object? sender, EventArgs e)
    {
        if (sender is not WidgetContainer container) return;
        RemoveWidget(container);
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isEditMode) return;
        // Only deselect if clicking directly on canvas, not on a widget
        if (e.Source is Canvas)
        {
            DeselectAll();
            HideSidePanel();
        }
    }

    private void SelectContainer(WidgetContainer container)
    {
        DeselectAll();
        _selectedContainer = container;
        container.SetSelected(true);
        ShowFunctionSelection(container);
    }

    private void DeselectAll()
    {
        foreach (var container in _widgetContainers)
        {
            container.SetSelected(false);
        }
        _selectedContainer = null;
    }

    public void EnterEditMode()
    {
        _isEditMode = true;
        UpdateClickThrough();
        var overlay = this.FindControl<Canvas>("EditOverlay");
        var toolbar = this.FindControl<Border>("EditToolbar");

        if (overlay != null) overlay.IsVisible = true;
        if (toolbar != null) toolbar.IsVisible = true;

        DrawGrid();
        foreach (var container in _widgetContainers)
        {
            container.SetEditMode(true);
        }

        EditModeChanged?.Invoke(this, true);
    }

    public void ExitEditMode()
    {
        _isEditMode = false;
        var overlay = this.FindControl<Canvas>("EditOverlay");
        var toolbar = this.FindControl<Border>("EditToolbar");

        if (overlay != null) overlay.IsVisible = false;
        if (toolbar != null) toolbar.IsVisible = false;

        ClearGrid();
        DeselectAll();
        HideSidePanel();

        // Remove placeholder widgets without function
        RemovePlaceholderWidgets();

        foreach (var container in _widgetContainers)
        {
            container.SetEditMode(false);
        }

        SaveLayout();
        UpdateClickThrough();
        EditModeChanged?.Invoke(this, false);
    }

    private void RemovePlaceholderWidgets()
    {
        var placeholders = _widgetContainers
            .Where(c => c.Config.WidgetId == "builtin.placeholder")
            .ToList();

        foreach (var container in placeholders)
        {
            RemoveWidget(container);
        }
    }

    private void DrawGrid()
    {
        var overlay = this.FindControl<Canvas>("EditOverlay");
        if (overlay == null) return;

        overlay.Children.Clear();

        var brush = new SolidColorBrush(Color.Parse("#20FFFFFF"));
        var minorBrush = new SolidColorBrush(Color.Parse("#10FFFFFF"));

        // Major grid lines every 80px
        for (double x = 0; x < Width; x += GridSize)
        {
            overlay.Children.Add(new Line
            {
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, Height),
                Stroke = brush,
                StrokeThickness = 1
            });
        }

        for (double y = 0; y < Height; y += GridSize)
        {
            overlay.Children.Add(new Line
            {
                StartPoint = new Point(0, y),
                EndPoint = new Point(Width, y),
                Stroke = brush,
                StrokeThickness = 1
            });
        }

        // Minor grid lines every 20px
        for (double x = 0; x < Width; x += 20)
        {
            if (x % GridSize == 0) continue;
            overlay.Children.Add(new Line
            {
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, Height),
                Stroke = minorBrush,
                StrokeThickness = 0.5
            });
        }

        for (double y = 0; y < Height; y += 20)
        {
            if (y % GridSize == 0) continue;
            overlay.Children.Add(new Line
            {
                StartPoint = new Point(0, y),
                EndPoint = new Point(Width, y),
                Stroke = minorBrush,
                StrokeThickness = 0.5
            });
        }
    }

    private void ClearGrid()
    {
        var overlay = this.FindControl<Canvas>("EditOverlay");
        overlay?.Children.Clear();
    }

    private void ShowSizeLibrary()
    {
        var sidePanel = this.FindControl<Border>("SidePanel");
        var title = this.FindControl<TextBlock>("SidePanelTitle");
        var content = this.FindControl<StackPanel>("SidePanelContent");

        if (sidePanel == null || title == null || content == null) return;

        title.Text = "添加组件 - 选择尺寸";
        content.Children.Clear();

        var sizes = GetAllSizes();
        var groups = sizes.GroupBy(s => GetSizeCategory(s));

        foreach (var group in groups)
        {
            content.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 8, 0, 4)
            });

            foreach (var size in group)
            {
                var button = new Button
                {
                    Content = $"{size.Name} ({size.Width}x{size.Height})",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    Tag = size
                };
                button.Click += (_, _) => AddPlaceholderWidget(size);
                content.Children.Add(button);
            }
        }

        sidePanel.IsVisible = true;
    }

    private void ShowFunctionSelection(WidgetContainer container)
    {
        var sidePanel = this.FindControl<Border>("SidePanel");
        var title = this.FindControl<TextBlock>("SidePanelTitle");
        var content = this.FindControl<StackPanel>("SidePanelContent");

        if (sidePanel == null || title == null || content == null) return;

        title.Text = "选择功能";
        content.Children.Clear();

        var supportedWidgets = GetSupportedWidgetsForSize(container.CurrentSize);

        foreach (var widget in supportedWidgets)
        {
            var button = new Button
            {
                Content = $"{widget.Name} - {widget.Description}",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Tag = widget
            };
            button.Click += (_, _) =>
            {
                container.SetWidget(widget);
                ShowWidgetSettings(container);
            };
            content.Children.Add(button);
        }

        sidePanel.IsVisible = true;
    }

    private void ShowWidgetSettings(WidgetContainer container)
    {
        var sidePanel = this.FindControl<Border>("SidePanel");
        var title = this.FindControl<TextBlock>("SidePanelTitle");
        var content = this.FindControl<StackPanel>("SidePanelContent");

        if (sidePanel == null || title == null || content == null) return;

        title.Text = "组件设置";
        content.Children.Clear();

        var settingsView = container.Widget.CreateSettingsView(container.Config.Settings);
        if (settingsView != null)
        {
            content.Children.Add(settingsView);
        }
        else
        {
            content.Children.Add(new TextBlock { Text = "此组件没有可设置项" });
        }
    }

    private void HideSidePanel()
    {
        var sidePanel = this.FindControl<Border>("SidePanel");
        if (sidePanel != null) sidePanel.IsVisible = false;
    }

    private void AddPlaceholderWidget(WidgetSize size)
    {
        var canvas = this.FindControl<Canvas>("WidgetsCanvas");
        if (canvas == null) return;

        var screen = Screens.Primary;
        var maxX = screen?.Bounds.Width ?? 1920;
        var maxY = screen?.Bounds.Height ?? 1080;

        var config = new WidgetInstanceConfig
        {
            WidgetId = "builtin.placeholder",
            SizeId = size.Id,
            PosX = Math.Floor((Width / 2 - size.Width / 2) / GridSize) * GridSize,
            PosY = Math.Floor((Height / 2 - size.Height / 2) / GridSize) * GridSize,
            PanelId = _configService.Panels.FirstOrDefault()?.Id ?? "default"
        };

        var panel = _configService.Panels.FirstOrDefault();
        if (panel == null)
        {
            panel = new PanelConfig { Name = "默认面板" };
            _configService.Panels.Add(panel);
        }
        panel.Widgets.Add(config);

        CreateWidgetContainer(config, canvas, maxX, maxY);
        var container = _widgetContainers.Last();
        SelectContainer(container);
    }

    private void RemoveWidget(WidgetContainer container)
    {
        foreach (var panel in _configService.Panels)
        {
            if (panel.Widgets.Remove(container.Config))
                break;
        }

        var canvas = this.FindControl<Canvas>("WidgetsCanvas");
        canvas?.Children.Remove(container);
        _widgetContainers.Remove(container);

        if (_selectedContainer == container)
        {
            _selectedContainer = null;
            HideSidePanel();
        }
    }

    private void ResetLayout()
    {
        _configService.ResetToDefault();
        LoadWidgets();
    }

    private void OpenAppearance()
    {
        var settingsWindow = new AppSettingsWindow(_services);
        settingsWindow.Show();
    }

    private void ClearAllWidgets()
    {
        foreach (var panel in _configService.Panels)
        {
            panel.Widgets.Clear();
        }

        LoadWidgets();
    }

    private void Undo()
    {
        // TODO: Implement undo/redo stack
    }

    private void Redo()
    {
        // TODO: Implement undo/redo stack
    }

    private void SaveLayout()
    {
        foreach (var container in _widgetContainers)
        {
            container.SavePosition();
        }
        _configService.Save();
    }

    private static IEnumerable<WidgetSize> GetAllSizes()
    {
        return new[]
        {
            new WidgetSize { Id = "mini-bar", Name = "迷你条", Width = 100, Height = 40, Type = WidgetSizeType.Small },
            new WidgetSize { Id = "small-vbar", Name = "小竖条", Width = 40, Height = 100, Type = WidgetSizeType.Small },
            new WidgetSize { Id = "small-hbar", Name = "小长条", Width = 180, Height = 40, Type = WidgetSizeType.Small },
            new WidgetSize { Id = "small-circle", Name = "小圆", Width = 80, Height = 80, Type = WidgetSizeType.Small, IsCircular = true },
            new WidgetSize { Id = "small-square", Name = "小方", Width = 80, Height = 80, Type = WidgetSizeType.Small },
            new WidgetSize { Id = "medium-hbar", Name = "中横条", Width = 200, Height = 100, Type = WidgetSizeType.Medium },
            new WidgetSize { Id = "medium-square", Name = "中方", Width = 140, Height = 140, Type = WidgetSizeType.Medium },
            new WidgetSize { Id = "medium-vbar", Name = "中竖条", Width = 100, Height = 200, Type = WidgetSizeType.Medium },
            new WidgetSize { Id = "large-square", Name = "大方", Width = 220, Height = 220, Type = WidgetSizeType.Large },
            new WidgetSize { Id = "large-vbar", Name = "大竖条", Width = 160, Height = 320, Type = WidgetSizeType.Large },
            new WidgetSize { Id = "large-hbar", Name = "大横条", Width = 320, Height = 160, Type = WidgetSizeType.Large }
        };
    }

    private static string GetSizeCategory(WidgetSize size)
    {
        return size.Type switch
        {
            WidgetSizeType.Small => "小类型",
            WidgetSizeType.Medium => "中类型",
            WidgetSizeType.Large => "大类型",
            _ => "其他"
        };
    }

    private IEnumerable<IWidget> GetSupportedWidgetsForSize(WidgetSize size)
    {
        return _widgetRegistry.GetAllWidgets()
            .Where(w => w.Id != "builtin.placeholder")
            .Where(w => w.SupportedSizes.Any(s => s.Id == size.Id));
    }
}
