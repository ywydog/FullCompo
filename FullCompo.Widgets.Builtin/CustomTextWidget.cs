using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

namespace FullCompo.Widgets.Builtin;

public class CustomTextWidget : WidgetBase
{
    public override string Id => "builtin.customtext";
    public override string Name => "自定义文本";
    public override string Description => "显示自定义文本";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        new WidgetSize { Id = "text-small", Name = "小", Type = WidgetSizeType.Small, Columns = 1, Rows = 1 },
        new WidgetSize { Id = "text-medium", Name = "中", Type = WidgetSizeType.Medium, Columns = 2, Rows = 1 },
        new WidgetSize { Id = "text-large", Name = "大", Type = WidgetSizeType.Large, Columns = 2, Rows = 2 }
    };

    public override WidgetSettings CreateDefaultSettings()
    {
        var settings = new WidgetSettings();
        settings.SetValue("text", "自定义文本");
        settings.SetValue("fontSize", 16.0);
        return settings;
    }

    public override Control CreateView(WidgetContext context)
    {
        var text = context.Settings.GetValue("text", "自定义文本");
        var fontSize = context.Settings.GetValue("fontSize", 16.0);

        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
    }

    public override Control? CreateSettingsView(WidgetSettings settings)
    {
        var stack = new StackPanel { Spacing = 8 };

        var textBox = new TextBox
        {
            Text = settings.GetValue("text", "自定义文本") ?? "自定义文本",
            Watermark = "文本内容"
        };
        textBox.TextChanged += (_, _) => settings.SetValue("text", textBox.Text ?? "");

        var fontSizeBox = new NumericUpDown
        {
            Value = (decimal?)settings.GetValue("fontSize", 16.0),
            Minimum = 8,
            Maximum = 72,
            Increment = 1
        };
        fontSizeBox.ValueChanged += (_, _) => settings.SetValue("fontSize", (double?)fontSizeBox.Value ?? 16.0);

        stack.Children.Add(new TextBlock { Text = "文本" });
        stack.Children.Add(textBox);
        stack.Children.Add(new TextBlock { Text = "字号" });
        stack.Children.Add(fontSizeBox);

        return stack;
    }
}
