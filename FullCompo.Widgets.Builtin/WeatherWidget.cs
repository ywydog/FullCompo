using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.Widgets.Builtin;

public class WeatherWidget : WidgetBase
{
    public override string Id => "builtin.weather";
    public override string Name => "天气";
    public override string Description => "显示当前天气";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        WidgetSizePresets.ShortBar,
        WidgetSizePresets.Square
    };

    public override Control CreateView(WidgetContext context)
    {
        var weatherService = context.Services.GetRequiredService<IWeatherService>();

        var textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold,
            Text = FormatWeather(weatherService.CurrentWeather),
            Foreground = GetForegroundBrush()
        };

        weatherService.WeatherUpdated += (_, _) =>
        {
            textBlock.Text = FormatWeather(weatherService.CurrentWeather);
        };

        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = textBlock
        };
    }

    private static string FormatWeather(WeatherData data)
    {
        if (string.IsNullOrEmpty(data.City))
        {
            return "☁ --";
        }
        return $"{data.Icon} {data.Temperature:F0}°C";
    }

    private static IBrush GetForegroundBrush()
    {
        if (Application.Current?.Resources.TryGetValue("ThemeForegroundColor", out var value) == true && value is IBrush brush)
        {
            return brush;
        }
        return new SolidColorBrush(Colors.White);
    }
}
