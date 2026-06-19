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
        new WidgetSize { Id = "weather-small", Name = "小", Type = WidgetSizeType.Small, Columns = 1, Rows = 1 }
    };

    public override Control CreateView(WidgetContext context)
    {
        var textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Text = "☁ 27°C"
        };

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 4),
            Child = textBlock
        };
    }
}
