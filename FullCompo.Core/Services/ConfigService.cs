using System.Text.Json;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Helpers;
using FullCompo.Shared.Models;

namespace FullCompo.Core.Services;

public class ConfigService : IConfigService
{
    public AppSettings AppSettings { get; private set; } = new();
    public List<PanelConfig> Panels { get; private set; } = new();

    public void Load()
    {
        var configDir = GetConfigDirectory();
        Directory.CreateDirectory(configDir);

        var settingsPath = Path.Combine(configDir, "settings.json");
        var panelsPath = Path.Combine(configDir, "panels.json");

        var isFirstRun = !File.Exists(settingsPath) && !File.Exists(panelsPath);

        try
        {
            AppSettings = File.Exists(settingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath), JsonHelper.Options) ?? new AppSettings()
                : new AppSettings();
        }
        catch
        {
            AppSettings = new AppSettings();
        }

        try
        {
            Panels = File.Exists(panelsPath)
                ? JsonSerializer.Deserialize<List<PanelConfig>>(File.ReadAllText(panelsPath), JsonHelper.Options) ?? new List<PanelConfig>()
                : new List<PanelConfig>();
        }
        catch
        {
            Panels = new List<PanelConfig>();
        }

        if (Panels.Count == 0)
        {
            ResetToDefault();
        }

        AppSettings.IsFirstRun = isFirstRun;
    }

    public void Save()
    {
        var configDir = GetConfigDirectory();
        Directory.CreateDirectory(configDir);

        var settingsPath = Path.Combine(configDir, "settings.json");
        var panelsPath = Path.Combine(configDir, "panels.json");

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(AppSettings, JsonHelper.Options));
        File.WriteAllText(panelsPath, JsonSerializer.Serialize(Panels, JsonHelper.Options));
    }

    public void ResetToDefault()
    {
        AppSettings = new AppSettings();

        // 默认布局：右上角横条面板内从左到右依次放置时钟、日期、天气。
        // 坐标相对于 top-right 停靠窗口的左上角（窗口高度固定 140 DIP）。
        const double left = 10;
        const double top = 20;
        const double gap = 12;

        var clockX = left;
        var dateX = clockX + 200 + gap; // medium-hbar width
        var weatherX = dateX + 180 + gap; // small-hbar width

        Panels = new List<PanelConfig>
        {
            new()
            {
                Name = "默认面板",
                Widgets = new List<WidgetInstanceConfig>
                {
                    new() { WidgetId = "builtin.clock", SizeId = "medium-hbar", PosX = clockX, PosY = top },
                    new() { WidgetId = "builtin.date", SizeId = "small-hbar", PosX = dateX, PosY = top + 30 },
                    new() { WidgetId = "builtin.weather", SizeId = "medium-hbar", PosX = weatherX, PosY = top }
                }
            }
        };
        Save();
    }

    public string GetConfigDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FullCompo", "data");
    }
}
