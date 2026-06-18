using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FullCompo.App.Views;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Widgets.Builtin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FullCompo.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private IThemeService _themeService = null!;
    private IPanelService _panelService = null!;
    private TrayIcon? _trayIcon;

    public App(IServiceProvider services)
    {
        _services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterBuiltinWidgets();

        _themeService = _services.GetRequiredService<IThemeService>();
        _panelService = _services.GetRequiredService<IPanelService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _panelService.CreateOrUpdatePanels();
            try
            {
                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                _services.GetRequiredService<ILogger<App>>().LogWarning(ex, "Failed to setup tray icon");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterBuiltinWidgets()
    {
        var registry = _services.GetRequiredService<IWidgetRegistry>();
        var provider = _services.GetRequiredService<BuiltinWidgetProvider>();
        registry.RegisterRange(provider.GetWidgets());
    }

    private void SetupTrayIcon()
    {
        var menu = new NativeMenu();

        var editModeItem = new NativeMenuItem("编辑模式");
        editModeItem.Click += (_, _) => ToggleEditMode();

        var settingsItem = new NativeMenuItem("设置");
        settingsItem.Click += (_, _) => OpenSettings();

        var exitItem = new NativeMenuItem("退出");
        exitItem.Click += (_, _) => Shutdown();

        menu.Add(editModeItem);
        menu.Add(settingsItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "全面组件",
            Menu = menu,
            IsVisible = true
        };

        LoadTrayIcon();

        var trayIcons = new TrayIcons { _trayIcon };
        TrayIcon.SetIcons(this, trayIcons);
    }

    private void LoadTrayIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://FullCompo.App/Assets/logo.png"));
            var bitmap = new Bitmap(stream);
            _trayIcon!.Icon = new WindowIcon(bitmap);
        }
        catch
        {
            // Logo not found, use default empty icon
        }
    }

    private void ToggleEditMode()
    {
        if (_panelService.IsEditMode)
        {
            _panelService.ExitEditMode();
        }
        else
        {
            _panelService.EnterEditMode();
        }
    }

    private void OpenSettings()
    {
        var window = new AppSettingsWindow(_services);
        window.Show();
    }

    private void Shutdown()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
