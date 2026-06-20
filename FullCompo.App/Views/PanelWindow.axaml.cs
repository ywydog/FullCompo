using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using FullCompo.App.Controls;
using FullCompo.App.Helpers;
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

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public PanelWindow(PanelConfig config, IServiceProvider services)
    {
        _config = config;
        _services = services;
        _widgetRegistry = services.GetRequiredService<IWidgetRegistry>();
        _configService = services.GetRequiredService<IConfigService>();
        _logger = services.GetRequiredService<ILogger<PanelWindow>>();

        InitializeComponent();
        SetupContextMenu();
        ReloadLayout();
        UpdatePosition();

        Opened += (_, _) =>
        {
            UpdatePosition();
            ApplyClickThrough();
        };
    }

    private double GetSpacingPixels()
    {
        try
        {
            return _configService.AppSettings.WidgetSpacing * 16;
        }
        catch
        {
            return 8;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupGrid()
    {
        WidgetGrid.Children.Clear();
        WidgetGrid.ColumnDefinitions.Clear();
        WidgetGrid.RowDefinitions.Clear();

        for (var i = 0; i < _config.Columns; i++)
        {
            WidgetGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        var maxRow = _config.Widgets.Any() ? _config.Widgets.Max(w => w.Row + w.RowSpan) : 1;
        for (var i = 0; i < maxRow; i++)
        {
            WidgetGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }
    }

    public void ReloadLayout()
    {
        _widgetHosts.Clear();
        SetupGrid();

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
                var spacing = GetSpacingPixels();
                view.Margin = new Thickness(spacing / 2);

                var host = new WidgetHost(this, widgetConfig, widget);
                host.SetWidgetView(view);

                Grid.SetColumn(host, widgetConfig.Column);
                Grid.SetRow(host, widgetConfig.Row);
                Grid.SetColumnSpan(host, widgetConfig.ColumnSpan);
                Grid.SetRowSpan(host, widgetConfig.RowSpan);

                WidgetGrid.Children.Add(host);
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

            var spacing = GetSpacingPixels();
            var totalWidth = _config.Columns * _config.CellWidth + (_config.Columns - 1) * spacing + _config.MarginLeft + _config.MarginRight + 16;
            var maxRow = _config.Widgets.Any() ? _config.Widgets.Max(w => w.Row + w.RowSpan) : 1;
            var totalHeight = maxRow * _config.CellHeight + (maxRow - 1) * spacing + _config.MarginTop + _config.MarginBottom + 16;

            Width = totalWidth;
            Height = totalHeight;

            var position = _config.DockMode switch
            {
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
                    bounds.X + bounds.Width - (int)totalWidth - (int)_config.MarginRight,
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
        PanelBorder.IsHitTestVisible = isEditMode;
        PanelBorder.Opacity = isEditMode ? 1.0 : _services.GetRequiredService<IThemeService>().CurrentTheme.Opacity;

        foreach (var host in _widgetHosts)
        {
            host.SetEditMode(isEditMode);
        }

        ApplyClickThrough();
    }

    private void ApplyClickThrough()
    {
        try
        {
            var clickThrough = _configService.AppSettings.ClickThrough && !IsEditMode;

            // Internal hit-test handling
            PanelBorder.IsHitTestVisible = !clickThrough || IsEditMode;
            WidgetGrid.IsHitTestVisible = !clickThrough || IsEditMode;
            foreach (var host in _widgetHosts)
            {
                host.IsHitTestVisible = !clickThrough || IsEditMode;
            }

            // Windows OS-level click-through
            if (OperatingSystem.IsWindows())
            {
                var handle = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    var exStyle = GetWindowLong(handle, GWL_EXSTYLE);
                    if (clickThrough)
                    {
                        SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
                    }
                    else
                    {
                        SetWindowLong(handle, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply click-through");
            AppLog.WriteException("PanelWindow.ApplyClickThrough", ex);
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
    }

    private void ShowPanelSettings()
    {
        var dialog = new PanelSettingsDialog(_config);
        dialog.Closed += (_, _) =>
        {
            SaveLayout();
            ReloadLayout();
            UpdatePosition();
        };
        dialog.Show(this);
    }

    internal void RemoveWidget(WidgetInstanceConfig config)
    {
        _config.Widgets.Remove(config);
        ReloadLayout();
        SaveLayout();
    }
}
