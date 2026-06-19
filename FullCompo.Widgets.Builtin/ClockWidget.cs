using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

namespace FullCompo.Widgets.Builtin;

public class ClockWidget : WidgetBase
{
    public override string Id => "builtin.clock";
    public override string Name => "时钟";
    public override string Description => "显示当前时间";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        new WidgetSize { Id = "1x1", Name = "方形", Type = WidgetSizeType.Small, Columns = 1, Rows = 1, Width = 120, Height = 120 },
        new WidgetSize { Id = "2x1", Name = "横条", Type = WidgetSizeType.Medium, Columns = 2, Rows = 1, Width = 248, Height = 120 },
        new WidgetSize { Id = "2x2", Name = "大", Type = WidgetSizeType.Large, Columns = 2, Rows = 2, Width = 248, Height = 248 }
    };

    public override Control CreateView(WidgetContext context)
    {
        var isLarge = context.CurrentSize.Rows >= 2;
        var fontSize = isLarge ? 36 : 20;

        var textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            Text = DateTime.Now.ToString("HH:mm:ss")
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => textBlock.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();

        return textBlock;
    }
}
