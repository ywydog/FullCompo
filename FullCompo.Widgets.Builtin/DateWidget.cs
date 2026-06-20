using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

namespace FullCompo.Widgets.Builtin;

public class DateWidget : WidgetBase
{
    public override string Id => "builtin.date";
    public override string Name => "日期";
    public override string Description => "显示当前日期和星期";

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
            Foreground = GetForegroundBrush()
        };

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = textBlock
        };

        textBlock.Text = FormatDate();

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => textBlock.Text = FormatDate();
        timer.Start();

        return viewbox;
    }

    private static string FormatDate()
    {
        // 示例：周五 06/30
        return DateTime.Now.ToString("ddd MM/dd");
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
