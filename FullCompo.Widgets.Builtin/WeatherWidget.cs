using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;
using FullCompo.Shared.Models.Weather;

namespace FullCompo.Widgets.Builtin;

public class WeatherWidget : WidgetBase
{
    public override string Id => "builtin.weather";
    public override string Name => "天气";
    public override string Description => "显示当前天气";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        new WidgetSize { Id = "small-square", Name = "小方", Type = WidgetSizeType.Small, Columns = 1, Rows = 1, Width = 80, Height = 80 },
        new WidgetSize { Id = "medium-hbar", Name = "中横条", Type = WidgetSizeType.Medium, Columns = 2, Rows = 1, Width = 200, Height = 100 },
        new WidgetSize { Id = "medium-square", Name = "中方", Type = WidgetSizeType.Medium, Columns = 2, Rows = 2, Width = 140, Height = 140 },
        new WidgetSize { Id = "large-square", Name = "大方", Type = WidgetSizeType.Large, Columns = 3, Rows = 3, Width = 220, Height = 220 }
    };

    public override Control CreateView(WidgetContext context)
    {
        var weatherService = context.GetService<IWeatherService>();
        var foreground = GetThemeBrush("ThemeForegroundBrush");
        var secondaryForeground = GetThemeBrush("ThemeSecondaryForegroundBrush");
        var accent = GetThemeBrush("ThemeAccentBrush");

        var isLarge = context.CurrentSize.Rows >= 2;
        var isWide = context.CurrentSize.Columns >= 2 && context.CurrentSize.Rows == 1;

        var iconText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = isLarge ? 48 : isWide ? 32 : 28,
            Foreground = accent,
            Text = "☁️"
        };

        var tempText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = isLarge ? 24 : isWide ? 20 : 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = foreground,
            Text = "--°"
        };

        var descText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = isLarge ? 14 : isWide ? 12 : 11,
            Foreground = secondaryForeground,
            Text = "加载中..."
        };

        var cityText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = isLarge ? 12 : 10,
            Foreground = secondaryForeground,
            Text = ""
        };

        var root = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (isWide)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 12
            };
            panel.Children.Add(iconText);
            var textPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 2
            };
            textPanel.Children.Add(tempText);
            textPanel.Children.Add(descText);
            panel.Children.Add(textPanel);
            root.Children.Add(panel);
        }
        else
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 2
            };
            panel.Children.Add(iconText);
            panel.Children.Add(tempText);
            panel.Children.Add(descText);
            if (isLarge) panel.Children.Add(cityText);
            root.Children.Add(panel);
        }

        void UpdateView(WeatherInfo? info)
        {
            if (info == null)
            {
                descText.Text = "未获取";
                return;
            }

            tempText.Text = info.Current.Temperature.ToString();
            descText.Text = info.Current.WeatherText;
            iconText.Text = WeatherCodeToIcon(info.Current.WeatherCode);
            cityText.Text = context.Settings.GetValue<string>("cityName", "") ?? "";
        }

        UpdateView(weatherService?.LastWeatherInfo);

        if (weatherService != null)
        {
            weatherService.WeatherUpdated += (_, info) =>
            {
                Dispatcher.UIThread.Post(() => UpdateView(info));
            };
        }

        return root;
    }

    private static string WeatherCodeToIcon(int code)
    {
        return code switch
        {
            0 => "☀️",
            1 => "⛅",
            2 => "☁️",
            >= 3 and <= 5 => "⛈️",
            6 => "🌨️",
            >= 7 and <= 12 => "🌧️",
            >= 13 and <= 17 => "❄️",
            18 => "🌫️",
            19 => "🌧️",
            >= 20 and <= 31 => "🌪️",
            >= 53 => "😷",
            _ => "🌤️"
        };
    }
}
