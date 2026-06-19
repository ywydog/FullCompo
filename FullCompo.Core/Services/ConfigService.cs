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

        // 默认布局放在屏幕右上角（以 1920×1080 为参考，留出 16px 边距）
        const double screenRight = 1920 - 16;
        const double rightColWidth = 140; // medium-square
        const double leftColWidth = 200;  // medium-hbar
        const double gap = 8;
        const double top = 16;
        const double rowHeight = 140;

        var rightColX = screenRight - rightColWidth;
        var leftColX = rightColX - gap - leftColWidth;

        Panels = new List<PanelConfig>
        {
            new()
            {
                Name = "默认面板",
                Widgets = new List<WidgetInstanceConfig>
                {
                    new() { WidgetId = "builtin.date", SizeId = "medium-hbar", PosX = leftColX, PosY = top },
                    new() { WidgetId = "builtin.clock", SizeId = "medium-square", PosX = rightColX, PosY = top },
                    new() { WidgetId = "builtin.weather", SizeId = "medium-hbar", PosX = leftColX, PosY = top + rowHeight + gap },
                    new() { WidgetId = "builtin.search", SizeId = "medium-hbar", PosX = rightColX, PosY = top + rowHeight + gap }
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
