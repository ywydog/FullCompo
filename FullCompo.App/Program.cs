using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using FullCompo.App.Helpers;
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
    private static Mutex? _singleInstanceMutex;
    private const string MutexName = "FullCompo_SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        // Single instance detection
        _singleInstanceMutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            // Already running - notify user and exit
            ShowAlreadyRunningMessage();
            return;
        }

        RegisterGlobalExceptionHandlers();
        AppLog.EnsureDirectory();
        AppLog.Write("Starting FullCompo...");

        IHost? host = null;
        try
        {
            try
            {
                VelopackApp.Build().Run();
                AppLog.Write("Velopack initialized");
            }
            catch (Exception vpEx)
            {
                AppLog.Write($"Velopack init skipped: {vpEx.Message}");
            }

            AppLog.Write("Building host...");
            host = CreateHostBuilder(args).Build();
            AppLog.Write("Host built");

            // Pre-load configuration before starting Avalonia
            AppLog.Write("Loading config...");
            var configService = host.Services.GetRequiredService<IConfigService>();
            configService.Load();
            AppLog.Write("Config loaded");

            AppLog.Write("Loading themes...");
            var themeService = host.Services.GetRequiredService<IThemeService>();
            themeService.LoadThemes();
            AppLog.Write("Themes loaded");
            // Note: ApplyTheme is called later in App.OnFrameworkInitializationCompleted
            // because Application.Current is null here.

            AppLog.Write("Starting Avalonia...");
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
            WriteCrashLog("Main", ex);
            Environment.Exit(1);
        }
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            WriteCrashLog("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    private static void WriteCrashLog(string context, Exception? ex)
    {
        if (ex == null) return;

        try
        {
            AppLog.EnsureDirectory();
            var fileName = $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log";
            var path = Path.Combine(AppLog.LogsDirectory, fileName);
            var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            File.WriteAllText(path, content);
        }
        catch
        {
            // Last-resort fallback to temp
            try
            {
                var fallback = Path.Combine(Path.GetTempPath(), "FullCompo_Crash.log");
                File.WriteAllText(fallback, $"[{context}] {ex}");
            }
            catch { }
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

    private static void ShowAlreadyRunningMessage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MessageBox(IntPtr.Zero, "FullCompo 已在运行中。", "FullCompo", 0x40); // MB_ICONINFORMATION
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
