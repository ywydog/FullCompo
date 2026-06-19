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
        new WidgetSize { Id = "date-small", Name = "小", Type = WidgetSizeType.Small, Columns = 1, Rows = 1 }
    };

    public override Control CreateView(WidgetContext context)
    {
        var textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Text = DateTime.Now.ToString("ddd MM/dd")
        };

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => textBlock.Text = DateTime.Now.ToString("ddd MM/dd");
        timer.Start();

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 4),
            Child = textBlock
        };
    }
}
