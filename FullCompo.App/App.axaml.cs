using System;
using System.IO;
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
        var logger = _services.GetService<ILogger<App>>();
        try
        {
            logger?.LogInformation("Initializing application lifetime");
            RegisterBuiltinWidgets();

            _themeService = _services.GetRequiredService<IThemeService>();
            _panelService = _services.GetRequiredService<IPanelService>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                InitializeDesktopLifetime(desktop);
            }

            base.OnFrameworkInitializationCompleted();
            logger?.LogInformation("Application lifetime initialized");
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Failed to initialize application lifetime");
            AppLog.WriteException("App.OnFrameworkInitializationCompleted", ex);
            throw;
        }
    }

    private void InitializeDesktopLifetime(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var logger = _services.GetService<ILogger<App>>();
        // For a tray-only application we keep the lifetime alive until explicitly shut down.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        IConfigService configService;
        try
        {
            configService = _services.GetRequiredService<IConfigService>();
            _themeService.ApplyTheme(configService.AppSettings.ThemeId);
            logger?.LogInformation("Theme applied: {ThemeId}", configService.AppSettings.ThemeId);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to apply theme");
            configService = _services.GetRequiredService<IConfigService>();
        }

        if (configService.AppSettings.IsFirstRun)
        {
            try
            {
                logger?.LogInformation("Showing welcome window");
                var welcome = new WelcomeWindow(_services)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true
                };
                welcome.Completed += (_, _) =>
                {
                    try
                    {
                        if (configService.AppSettings.ShowTrayIcon)
                        {
                            SetupTrayIcon();
                        }
                        _panelService.CreateOrUpdatePanels();
                    }
                    catch (Exception ex)
                    {
                        AppLog.WriteException("Create tray icon and panels after welcome", ex);
                    }
                };
                welcome.Closed += (_, _) =>
                {
                    if (configService.AppSettings.IsFirstRun)
                    {
                        // User closed welcome without finishing; exit instead of leaving a background app.
                        Shutdown();
                    }
                };
                welcome.Show();
                welcome.Activate();
                welcome.Focus();
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
            if (configService.AppSettings.ShowTrayIcon)
            {
                try
                {
                    SetupTrayIcon();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to setup tray icon");
                    AppLog.WriteException("SetupTrayIcon", ex);
                }
            }

            try
            {
                _panelService.CreateOrUpdatePanels();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to create panels");
                AppLog.WriteException("CreateOrUpdatePanels", ex);
            }
        }
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
            IsVisible = false
        };

        LoadTrayIcon();
        _trayIcon.IsVisible = _trayIcon.Icon != null;

        var trayIcons = new TrayIcons { _trayIcon };
        TrayIcon.SetIcons(this, trayIcons);
    }

    private void LoadTrayIcon()
    {
        try
        {
            var bitmap = LoadAppLogoBitmap();
            if (bitmap == null)
            {
                var logger = _services.GetService<ILogger<App>>();
                logger?.LogWarning("Logo asset not found; tray icon will be empty");
                return;
            }

            using (bitmap)
            {
                // Tray icons on Windows are typically 16x16/32x32; scale the app logo down.
                var scaled = bitmap.CreateScaledBitmap(new PixelSize(32, 32), BitmapInterpolationMode.HighQuality);
                _trayIcon!.Icon = new WindowIcon(scaled);
            }
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<App>>();
            logger?.LogWarning(ex, "Failed to load tray icon");
            AppLog.WriteException("LoadTrayIcon", ex);
        }
    }

    private Bitmap? LoadAppLogoBitmap()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://FullCompo/Assets/logo.png"));
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<App>>();
            logger?.LogWarning(ex, "Failed to load logo from Avalonia resource");
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
            if (File.Exists(path))
            {
                return new Bitmap(path);
            }
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<App>>();
            logger?.LogWarning(ex, "Failed to load logo from output directory");
        }

        return null;
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
        try
        {
            var window = new AppSettingsWindow(_services)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };
            window.Show();
            window.Activate();
            window.Focus();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<App>>();
            logger?.LogError(ex, "Failed to open settings window");
            AppLog.WriteException("OpenSettings", ex);
        }
    }

    private void Shutdown()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
