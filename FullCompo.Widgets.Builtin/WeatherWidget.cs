using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

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
        var textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold,
            Text = "☁ 27°C",
            Foreground = GetForegroundBrush()
        };

        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = textBlock
        };
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
