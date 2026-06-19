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
        try
        {
            File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting FullCompo...\n");

            try
            {
                VelopackApp.Build().Run();
                File.AppendAllText(logPath, "[OK] Velopack initialized\n");
            }
            catch (Exception vpEx)
            {
                File.AppendAllText(logPath, $"[WARN] Velopack init skipped: {vpEx.Message}\n");
            }

            File.AppendAllText(logPath, "[..] Building host...\n");
            host = CreateHostBuilder(args).Build();
            File.AppendAllText(logPath, "[OK] Host built\n");

            // Pre-load configuration before starting Avalonia
            File.AppendAllText(logPath, "[..] Loading config...\n");
            var configService = host.Services.GetRequiredService<IConfigService>();
            configService.Load();
            File.AppendAllText(logPath, "[OK] Config loaded\n");

            File.AppendAllText(logPath, "[..] Loading themes...\n");
            var themeService = host.Services.GetRequiredService<IThemeService>();
            themeService.LoadThemes();
            File.AppendAllText(logPath, "[OK] Themes loaded\n");
            // Note: ApplyTheme is called later in App.OnFrameworkInitializationCompleted
            // because Application.Current is null here.

            File.AppendAllText(logPath, "[..] Starting Avalonia...\n");
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
