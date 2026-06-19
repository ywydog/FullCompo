using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FullCompo.App.Controls;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FullCompo.App.Views;

public partial class PanelWindow : Window
{
    private readonly PanelConfig _config;
    private readonly IWidgetRegistry _widgetRegistry;
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly ILogger<PanelWindow> _logger;
    private readonly List<WidgetHost> _widgetHosts = new();

    public PanelConfig Config => _config;
    public bool IsEditMode { get; private set; }

    public PanelWindow(PanelConfig config, IServiceProvider services)
    {
        _config = config;
        _services = services;
        _widgetRegistry = services.GetRequiredService<IWidgetRegistry>();
        _configService = services.GetRequiredService<IConfigService>();
        _logger = services.GetRequiredService<ILogger<PanelWindow>>();

        InitializeComponent();
        SetupContextMenu();
        ApplyCornerRadius();
        ReloadLayout();

        Opened += (_, _) =>
        {
            UpdatePosition();
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ApplyCornerRadius()
    {
        var panelBorder = this.FindControl<Border>("PanelBorder");
        if (panelBorder != null)
        {
            panelBorder.CornerRadius = new CornerRadius(_config.CornerRadius);
        }
    }

    public void ReloadLayout()
    {
        _widgetHosts.Clear();
        var widgetStack = this.FindControl<StackPanel>("WidgetStack");
        if (widgetStack == null) return;

        widgetStack.Children.Clear();

        foreach (var widgetConfig in _config.Widgets)
        {
            var widget = _widgetRegistry.GetWidget(widgetConfig.WidgetId);
            if (widget == null)
            {
                _logger.LogWarning("Widget {WidgetId} not found", widgetConfig.WidgetId);
                continue;
            }

            var size = widget.SupportedSizes.FirstOrDefault(s => s.Columns == widgetConfig.ColumnSpan && s.Rows == widgetConfig.RowSpan)
                ?? widget.SupportedSizes.First();

            var context = new WidgetContext(
                widgetConfig.Id,
                _config.Id,
                size,
                _services,
                _services.GetRequiredService<ILoggerFactory>().CreateLogger(widget.GetType().FullName ?? widget.Id),
                widgetConfig.Settings);

            try
            {
                widget.OnActivated(context);
                var view = widget.CreateView(context);

                // Wrap view in a container with fixed height matching panel height
                var container = new Border
                {
                    Child = view,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    Height = _config.PanelHeight - 16, // minus padding
                };

                var host = new WidgetHost(this, widgetConfig, widget);
                host.SetWidgetView(container);

                widgetStack.Children.Add(host);
                _widgetHosts.Add(host);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load widget {WidgetId}", widgetConfig.WidgetId);
            }
        }

        SetEditMode(IsEditMode);
    }

    public void UpdatePosition()
    {
        try
        {
            var screen = Screens?.ScreenFromWindow(this);
            var bounds = screen?.Bounds ?? new PixelRect(0, 0, 1920, 1080);

            // Let SizeToContent handle the actual size, just position the window
            var totalWidth = Bounds.Width > 0 ? Bounds.Width : 400;
            var totalHeight = Bounds.Height > 0 ? Bounds.Height : _config.PanelHeight;

            var position = _config.DockMode switch
            {
                PanelDockMode.TopCenter => new PixelPoint(
                    bounds.X + (bounds.Width - (int)totalWidth) / 2,
                    bounds.Y + (int)_config.MarginTop),
                PanelDockMode.BottomCenter => new PixelPoint(
                    bounds.X + (bounds.Width - (int)totalWidth) / 2,
                    bounds.Y + bounds.Height - (int)totalHeight - (int)_config.MarginBottom),
                PanelDockMode.TopRightCorner => new PixelPoint(
                    bounds.X + bounds.Width - (int)totalWidth - (int)_config.MarginRight,
                    bounds.Y + (int)_config.MarginTop),
                PanelDockMode.TopLeftCorner => new PixelPoint(
                    bounds.X + (int)_config.MarginLeft,
                    bounds.Y + (int)_config.MarginTop),
                PanelDockMode.BottomRightCorner => new PixelPoint(
                    bounds.X + bounds.Width - (int)totalWidth - (int)_config.MarginRight,
                    bounds.Y + bounds.Height - (int)totalHeight - (int)_config.MarginBottom),
                PanelDockMode.BottomLeftCorner => new PixelPoint(
                    bounds.X + (int)_config.MarginLeft,
                    bounds.Y + bounds.Height - (int)totalHeight - (int)_config.MarginBottom),
                _ => new PixelPoint(
                    bounds.X + (bounds.Width - (int)totalWidth) / 2,
                    bounds.Y + (int)_config.MarginTop)
            };

            Position = position;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update panel position");
        }
    }

    public void SetEditMode(bool isEditMode)
    {
        IsEditMode = isEditMode;
        var panelBorder = this.FindControl<Border>("PanelBorder");
        if (panelBorder != null)
        {
            panelBorder.IsHitTestVisible = true; // Always allow interaction with panel
            panelBorder.Opacity = isEditMode ? 1.0 : _services.GetRequiredService<IThemeService>().CurrentTheme.Opacity;
        }

        ApplyClickThrough();

        foreach (var host in _widgetHosts)
        {
            host.SetEditMode(isEditMode);
        }
    }

    private void ApplyClickThrough()
    {
        var clickThrough = _configService.AppSettings.ClickThrough && !IsEditMode;

        if (clickThrough)
        {
            var panelBorder = this.FindControl<Border>("PanelBorder");
            if (panelBorder != null) panelBorder.IsHitTestVisible = false;
            var widgetStack = this.FindControl<StackPanel>("WidgetStack");
            if (widgetStack != null) widgetStack.IsHitTestVisible = false;
            foreach (var host in _widgetHosts)
            {
                host.IsHitTestVisible = false;
            }
        }
        else
        {
            var panelBorder = this.FindControl<Border>("PanelBorder");
            if (panelBorder != null) panelBorder.IsHitTestVisible = true;
            var widgetStack = this.FindControl<StackPanel>("WidgetStack");
            if (widgetStack != null) widgetStack.IsHitTestVisible = true;
            foreach (var host in _widgetHosts)
            {
                host.IsHitTestVisible = !IsEditMode; // In normal mode, widgets are interactive
            }
        }
    }

    public void SaveLayout()
    {
        _configService.Save();
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenu();

        var editModeItem = new MenuItem { Header = "编辑模式" };
        editModeItem.Click += (_, _) => ToggleEditMode();

        var addWidgetItem = new MenuItem { Header = "添加组件" };
        addWidgetItem.Click += (_, _) => ShowAddWidgetDialog();

        var panelSettingsItem = new MenuItem { Header = "面板设置" };
        panelSettingsItem.Click += (_, _) => ShowPanelSettings();

        contextMenu.Items.Add(editModeItem);
        contextMenu.Items.Add(addWidgetItem);
        contextMenu.Items.Add(panelSettingsItem);

        ContextMenu = contextMenu;
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

    private void ShowAddWidgetDialog()
    {
        var menu = new ContextMenu();
        foreach (var widget in _widgetRegistry.GetAllWidgets())
        {
            var item = new MenuItem { Header = widget.Name };
            item.Click += (_, _) => AddWidget(widget.Id);
            menu.Items.Add(item);
        }
        menu.Open(this);
    }

    private void AddWidget(string widgetId)
    {
        var widget = _widgetRegistry.GetWidget(widgetId);
        if (widget == null) return;

        var size = widget.SupportedSizes.First();
        var config = new WidgetInstanceConfig
        {
            WidgetId = widgetId,
            Column = 0,
            Row = 0,
            ColumnSpan = size.Columns,
            RowSpan = size.Rows,
            Settings = widget.CreateDefaultSettings()
        };

        _config.Widgets.Add(config);
        ReloadLayout();
        SaveLayout();

        // Reposition after layout change
        DispatcherTimer.RunOnce(() => UpdatePosition(), TimeSpan.FromMilliseconds(50));
    }

    private void ShowPanelSettings()
    {
        var dialog = new PanelSettingsDialog(_config);
        dialog.Closed += (_, _) =>
        {
            SaveLayout();
            ReloadLayout();
            ApplyCornerRadius();
            DispatcherTimer.RunOnce(() => UpdatePosition(), TimeSpan.FromMilliseconds(50));
        };
        dialog.Show(this);
    }

    internal void RemoveWidget(WidgetInstanceConfig config)
    {
        _config.Widgets.Remove(config);
        ReloadLayout();
        SaveLayout();
        DispatcherTimer.RunOnce(() => UpdatePosition(), TimeSpan.FromMilliseconds(50));
    }
}
