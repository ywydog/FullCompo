using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FullCompo.App.Views;

public partial class WidgetWindow : Window
{
    private readonly WidgetInstanceConfig _config;
    private readonly IWidget _widget;
    private readonly IWidgetRegistry _widgetRegistry;
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly ILogger<WidgetWindow> _logger;
    private WidgetSize _currentSize;

    private bool _isDragging;
    private Point _dragStart;
    private PixelPoint _positionStart;

    public WidgetInstanceConfig Config => _config;
    public bool IsEditMode { get; private set; }

    // Grid snap size (like phone home screen)
    private const double GridCellSize = 128; // 120 widget + 8 gap
    private const double SnapThreshold = 32;

    public WidgetWindow(WidgetInstanceConfig config, IWidget widget, IServiceProvider services)
    {
        _config = config;
        _widget = widget;
        _services = services;
        _widgetRegistry = services.GetRequiredService<IWidgetRegistry>();
        _configService = services.GetRequiredService<IConfigService>();
        _logger = services.GetRequiredService<ILogger<WidgetWindow>>();

        _currentSize = widget.SupportedSizes.FirstOrDefault(s => s.Id == config.SizeId)
            ?? widget.SupportedSizes.First();

        InitializeComponent();
        SetupSize();
        SetupContextMenu();
        LoadWidget();
        ApplyPosition();

        Opened += (_, _) => ApplyPosition();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupSize()
    {
        Width = _currentSize.Width;
        Height = _currentSize.Height;

        var border = this.FindControl<Border>("WidgetBorder");
        if (border != null && _currentSize.IsCircular)
        {
            border.CornerRadius = new CornerRadius(_currentSize.Width / 2);
        }
    }

    private void LoadWidget()
    {
        var contentGrid = this.FindControl<Grid>("ContentGrid");
        if (contentGrid == null) return;

        var context = new WidgetContext(
            _config.Id,
            _config.PanelId,
            _currentSize,
            _services,
            _services.GetRequiredService<ILoggerFactory>().CreateLogger(_widget.GetType().FullName ?? _widget.Id),
            _config.Settings);

        try
        {
            _widget.OnActivated(context);
            var view = _widget.CreateView(context);
            contentGrid.Children.Add(view);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load widget {WidgetId}", _widget.Id);
        }
    }

    private void ApplyPosition()
    {
        if (_config.PosX > 0 || _config.PosY > 0)
        {
            Position = new PixelPoint((int)_config.PosX, (int)_config.PosY);
        }
    }

    public void SetEditMode(bool isEditMode)
    {
        IsEditMode = isEditMode;
        var border = this.FindControl<Border>("WidgetBorder");
        if (border == null) return;

        if (isEditMode)
        {
            border.BorderBrush = new SolidColorBrush(Color.Parse("#80FFFFFF"));
            border.BorderThickness = new Thickness(2);
            border.Opacity = 1.0;
            Cursor = new Cursor(StandardCursorType.Hand);
        }
        else
        {
            border.BorderBrush = new SolidColorBrush(_services.GetRequiredService<IThemeService>().CurrentTheme.BorderColor);
            border.BorderThickness = new Thickness(1);
            border.Opacity = _services.GetRequiredService<IThemeService>().CurrentTheme.Opacity;
            Cursor = Cursor.Default;
        }
    }

    public void SavePosition()
    {
        _config.PosX = Position.X;
        _config.PosY = Position.Y;
        _configService.Save();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!IsEditMode) return;

        _isDragging = true;
        _dragStart = e.GetPosition(this);
        _positionStart = Position;
        e.Pointer.Capture(this);
        e.Handled = true;

        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(this);
        var delta = current - _dragStart;
        var newX = _positionStart.X + (int)delta.X;
        var newY = _positionStart.Y + (int)delta.Y;

        // Snap to grid if close enough
        var snappedX = Math.Round((double)newX / GridCellSize) * GridCellSize;
        var snappedY = Math.Round((double)newY / GridCellSize) * GridCellSize;

        if (Math.Abs(newX - snappedX) < SnapThreshold && Math.Abs(newY - snappedY) < SnapThreshold)
        {
            newX = (int)snappedX;
            newY = (int)snappedY;
        }

        Position = new PixelPoint((int)newX, (int)newY);
        e.Handled = true;

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        e.Pointer.Capture(null);
        SavePosition();
        e.Handled = true;

        base.OnPointerReleased(e);
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenu();

        var editModeItem = new MenuItem { Header = "编辑模式" };
        editModeItem.Click += (_, _) => ToggleEditMode();

        var resizeItem = new MenuItem { Header = "切换尺寸" };
        SetupResizeMenu(resizeItem);

        var settingsItem = new MenuItem { Header = "组件设置" };
        settingsItem.Click += (_, _) => OpenSettings();

        var deleteItem = new MenuItem { Header = "删除组件" };
        deleteItem.Click += (_, _) => DeleteWidget();

        contextMenu.Items.Add(editModeItem);
        contextMenu.Items.Add(resizeItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(deleteItem);

        ContextMenu = contextMenu;
    }

    private void SetupResizeMenu(MenuItem parent)
    {
        foreach (var size in _widget.SupportedSizes)
        {
            var item = new MenuItem { Header = $"{size.Name} ({size.Width}x{size.Height})" };
            var capturedSize = size;
            item.Click += (_, _) => ResizeWidget(capturedSize);
            parent.Items.Add(item);
        }
    }

    private void ResizeWidget(WidgetSize size)
    {
        _currentSize = size;
        _config.SizeId = size.Id;
        Width = size.Width;
        Height = size.Height;

        var border = this.FindControl<Border>("WidgetBorder");
        if (border != null && size.IsCircular)
        {
            border.CornerRadius = new CornerRadius(size.Width / 2);
        }
        else if (border != null)
        {
            border.CornerRadius = new CornerRadius(16);
        }

        // Reload widget content
        var contentGrid = this.FindControl<Grid>("ContentGrid");
        if (contentGrid != null)
        {
            contentGrid.Children.Clear();
            LoadWidget();
        }

        _configService.Save();
    }

    private void ToggleEditMode()
    {
        var panelService = _services.GetRequiredService<IPanelService>();
        if (IsEditMode)
        {
            panelService.ExitEditMode();
        }
        else
        {
            panelService.EnterEditMode();
        }
    }

    private void OpenSettings()
    {
        var settingsView = _widget.CreateSettingsView(_config.Settings);
        if (settingsView == null) return;

        var dialog = new SettingsDialog(_widget.Name, settingsView);
        dialog.Show(this);
    }

    private void DeleteWidget()
    {
        var panelService = _services.GetRequiredService<IPanelService>();
        panelService.RemoveWidget(_config);
        Close();
    }
}
