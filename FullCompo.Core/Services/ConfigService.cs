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
        Panels = new List<PanelConfig>
        {
            new()
            {
                Name = "默认面板",
                DockMode = PanelDockMode.TopRightCorner,
                MarginRight = 16,
                MarginTop = 48,
                Columns = 4,
                CellWidth = 80,
                CellHeight = 80,
                Spacing = 8,
                Widgets = new List<WidgetInstanceConfig>
                {
                    new() { WidgetId = "builtin.date", Column = 0, Row = 0, ColumnSpan = 2, RowSpan = 1, SizeId = "short-bar" },
                    new() { WidgetId = "builtin.weather", Column = 0, Row = 1, ColumnSpan = 2, RowSpan = 1, SizeId = "short-bar" },
                    new() { WidgetId = "builtin.clock", Column = 2, Row = 0, ColumnSpan = 2, RowSpan = 2, SizeId = "square" }
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
