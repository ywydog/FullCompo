using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

namespace FullCompo.Widgets.Builtin;

public class SearchWidget : WidgetBase
{
    public override string Id => "builtin.search";
    public override string Name => "搜索框";
    public override string Description => "快速搜索";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        new WidgetSize { Id = "search-small", Name = "小", Type = WidgetSizeType.Small, Columns = 2, Rows = 1 },
        new WidgetSize { Id = "search-medium", Name = "中", Type = WidgetSizeType.Medium, Columns = 3, Rows = 1 }
    };

    public override Control CreateView(WidgetContext context)
    {
        var textBox = new TextBox
        {
            Watermark = "搜索...",
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#22FFFFFF")),
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(0),
            FontSize = 14
        };

        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var query = Uri.EscapeDataString(textBox.Text);
                Process.Start(new ProcessStartInfo($"https://www.bing.com/search?q={query}") { UseShellExecute = true });
                textBox.Text = string.Empty;
            }
        };

        return textBox;
    }
}
