namespace FullCompo.Shared.Models;

public class AppSettings
{
    // === 常规 ===
    public string Language { get; set; } = "zh-CN";
    public bool RunOnStartup { get; set; }
    public bool ShowTrayIcon { get; set; } = true;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool IsFirstRun { get; set; } = true;

    // === 外观 ===
    public string ThemeId { get; set; } = "system";
    public double BackgroundOpacity { get; set; } = 0.92;
    public double ScaleFactor { get; set; } = 1.0;
    public double CornerRadius { get; set; } = 20;
    public string? CustomFontFamily { get; set; }

    // === 窗口 ===
    public string DockPosition { get; set; } = "top-right"; // free, top, top-left, top-right, bottom, bottom-left, bottom-right, left, right
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool HoverToFade { get; set; } = false;
    public double HoverOpacity { get; set; } = 0.3;

    // === 快捷键 ===
    public string EditModeShortcut { get; set; } = "Ctrl+Shift+E";
    public string SettingsShortcut { get; set; } = "Ctrl+Shift+S";

    // === 天气 ===
    public bool WeatherEnabled { get; set; } = true;
    public string WeatherCityId { get; set; } = "101010100"; // 默认北京
    public string WeatherCityName { get; set; } = "北京";
    public bool WeatherAutoLocation { get; set; } = true;
    public double WeatherLongitude { get; set; }
    public double WeatherLatitude { get; set; }
    public bool NoTLSWeatherRequests { get; set; }

    // === 更新 ===
    public bool AutoUpdate { get; set; } = true;
    public string UpdateChannel { get; set; } = "stable"; // stable, nightly

    // === 数据 ===
    public bool AutoBackup { get; set; } = true;
    public int MaxBackupCount { get; set; } = 5;
}
