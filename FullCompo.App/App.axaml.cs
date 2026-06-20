using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FullCompo.App.Helpers;
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
            // For a tray-only application we keep the lifetime alive until explicitly shut down.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            IConfigService configService;
            try
            {
                configService = _services.GetRequiredService<IConfigService>();
                _themeService.ApplyTheme(configService.AppSettings.ThemeId);
            }
            catch (Exception ex)
            {
                var logger = _services.GetService<ILogger<App>>();
                logger?.LogError(ex, "Failed to apply theme");
                configService = _services.GetRequiredService<IConfigService>();
            }

            if (configService.AppSettings.ShowTrayIcon)
            {
                try
                {
                    SetupTrayIcon();
                }
                catch (Exception ex)
                {
                    var logger = _services.GetService<ILogger<App>>();
                    logger?.LogError(ex, "Failed to setup tray icon");
                    AppLog.WriteException("SetupTrayIcon", ex);
                }
            }

            if (configService.AppSettings.IsFirstRun)
            {
                try
                {
                    var welcome = new WelcomeWindow(_services)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    welcome.Completed += (_, _) =>
                    {
                        try { _panelService.CreateOrUpdatePanels(); }
                        catch (Exception ex)
                        {
                            AppLog.WriteException("Create panels after welcome", ex);
                        }
                    };
                    welcome.Closed += (_, _) =>
                    {
                        if (configService.AppSettings.IsFirstRun)
                        {
                            // User closed welcome without finishing; still create panels.
                            try { _panelService.CreateOrUpdatePanels(); }
                            catch (Exception ex)
                            {
                                AppLog.WriteException("Create panels after welcome closed", ex);
                            }
                        }
                    };
                    welcome.Show();
                    welcome.Activate();
                }
                catch (Exception ex)
                {
                    AppLog.WriteException("Show welcome window", ex);
                    try { _panelService.CreateOrUpdatePanels(); }
                    catch (Exception ex2)
                    {
                        AppLog.WriteException("Create panels when welcome failed", ex2);
                    }
                }
            }
            else
            {
                try
                {
                    _panelService.CreateOrUpdatePanels();
                }
                catch (Exception ex)
                {
                    var logger = _services.GetService<ILogger<App>>();
                    logger?.LogError(ex, "Failed to create panels");
                    AppLog.WriteException("CreateOrUpdatePanels", ex);
                }
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
            // Tray icons on Windows are typically 16x16/32x32; scale the app logo down.
            var scaled = bitmap.CreateScaledBitmap(new PixelSize(32, 32), BitmapInterpolationMode.HighQuality);
            _trayIcon!.Icon = new WindowIcon(scaled);
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<App>>();
            logger?.LogWarning(ex, "Failed to load tray icon");
            AppLog.WriteException("LoadTrayIcon", ex);
        }
    }

    private void ToggleEditMode()
    {
        try
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
        catch (Exception ex)
        {
            AppLog.WriteException("Toggle edit mode", ex);
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
