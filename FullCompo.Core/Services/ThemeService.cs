using Avalonia;
using Avalonia.Media;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;

namespace FullCompo.Core.Services;

public class ThemeService : IThemeService
{
    private readonly List<ThemeConfig> _themes = new();

    public ThemeConfig CurrentTheme { get; private set; } = new();
    public IReadOnlyList<ThemeConfig> AvailableThemes => _themes;

    public event EventHandler<ThemeConfig>? ThemeChanged;

    public void LoadThemes()
    {
        _themes.Clear();
        _themes.AddRange(GetBuiltinThemes());

        if (_themes.Count > 0 && string.IsNullOrEmpty(CurrentTheme.Id))
        {
            CurrentTheme = _themes[0];
        }
    }

    public void ApplyTheme(string themeId)
    {
        var theme = _themes.FirstOrDefault(t => t.Id == themeId);
        if (theme == null) return;

        CurrentTheme = theme;
        ApplyToApplicationResources(theme);
        ThemeChanged?.Invoke(this, theme);
    }

    private static IEnumerable<ThemeConfig> GetBuiltinThemes()
    {
        return new[]
        {
            new ThemeConfig
            {
                Id = "dark",
                Name = "深色",
                BackgroundColor = Color.Parse("#E01E1E1E"),
                ForegroundColor = Colors.White,
                AccentColor = Color.Parse("#FF4A90E2"),
                BorderColor = Color.Parse("#55FFFFFF"),
                BorderThickness = 1,
                CornerRadius = 20,
                Opacity = 0.95
            },
            new ThemeConfig
            {
                Id = "light",
                Name = "浅色",
                BackgroundColor = Color.Parse("#F0F5F5F5"),
                ForegroundColor = Color.Parse("#FF1F1F1F"),
                AccentColor = Color.Parse("#FF0078D4"),
                BorderColor = Color.Parse("#33000000"),
                BorderThickness = 1,
                CornerRadius = 20,
                Opacity = 0.98
            },
            new ThemeConfig
            {
                Id = "glass",
                Name = "毛玻璃",
                BackgroundColor = Color.Parse("#CC232323"),
                ForegroundColor = Colors.White,
                AccentColor = Color.Parse("#FF00D4AA"),
                BorderColor = Color.Parse("#77FFFFFF"),
                BorderThickness = 1,
                CornerRadius = 20,
                Opacity = 0.9
            }
        };
    }

    private static void ApplyToApplicationResources(ThemeConfig theme)
    {
        var resources = Application.Current?.Resources;
        if (resources == null) return;

        resources["ThemeBackgroundColor"] = new SolidColorBrush(theme.BackgroundColor);
        resources["ThemeForegroundColor"] = new SolidColorBrush(theme.ForegroundColor);
        resources["ThemeAccentColor"] = new SolidColorBrush(theme.AccentColor);
        resources["ThemeBorderColor"] = new SolidColorBrush(theme.BorderColor);
        resources["ThemeBorderThickness"] = theme.BorderThickness;
        resources["ThemeCornerRadius"] = new CornerRadius(theme.CornerRadius);
        resources["ThemeFontSizeScale"] = theme.FontSizeScale;
        resources["ThemeOpacity"] = theme.Opacity;
    }
}
