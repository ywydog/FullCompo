using System.IO;
using Avalonia;
using Avalonia.Controls;
using FullCompo.App.Services;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Services;
using FullCompo.Widgets.Builtin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Velopack;

namespace FullCompo.App;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        IHost? host = null;
        var logPath = Path.Combine(Path.GetTempPath(), "FullCompo_Crash.log");
        void AppendLog(string message)
        {
            try { File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n"); }
            catch { }
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppendLog($"Unhandled exception: {e.ExceptionObject}");
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppendLog($"Unobserved task exception: {e.Exception}");
            e.SetObserved();
        };

        try
        {
            File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting FullCompo...\n");

            try
            {
                VelopackApp.Build().Run();
                AppendLog("[OK] Velopack initialized");
            }
            catch (Exception vpEx)
            {
                // Velopack may exit here if another instance is already running (single-instance).
                AppendLog($"[WARN] Velopack init skipped: {vpEx.Message}");
            }

            AppendLog("[..] Building host...");
            host = CreateHostBuilder(args).Build();
            AppendLog("[OK] Host built");

            // Pre-load configuration before starting Avalonia
            AppendLog("[..] Loading config...");
            var configService = host.Services.GetRequiredService<IConfigService>();
            configService.Load();
            AppendLog("[OK] Config loaded");

            AppendLog("[..] Loading themes...");
            var themeService = host.Services.GetRequiredService<IThemeService>();
            themeService.LoadThemes();
            AppendLog("[OK] Themes loaded");
            // Note: ApplyTheme is called later in App.OnFrameworkInitializationCompleted
            // because Application.Current is null here.

            AppendLog("[..] Starting Avalonia...");
            BuildAvaloniaApp(host.Services)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try
            {
                host?.Services.GetService<ILogger<Program>>()?.LogCritical(ex, "Application crashed");
            }
            catch { }
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.WriteAllText(logPath, $"[{timestamp}] Application crashed:\n{ex}");
            }
            catch { }
            Environment.Exit(1);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfigService, ConfigService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IWidgetRegistry, WidgetRegistry>();
                services.AddSingleton<IPanelService, PanelService>();
                services.AddSingleton<BuiltinWidgetProvider>();
                services.AddSingleton<IWeatherService, WeatherService>();

                services.AddLogging(builder =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "[HH:mm:ss] ";
                    });
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
    {
        return AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
