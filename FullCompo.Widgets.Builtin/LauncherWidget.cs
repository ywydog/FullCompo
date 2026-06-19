using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FullCompo.Widgets.Builtin;

public class LauncherWidget : WidgetBase
{
    public override string Id => "builtin.launcher";
    public override string Name => "快捷启动";
    public override string Description => "快捷启动应用或网址";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        new WidgetSize { Id = "launcher-small", Name = "小", Type = WidgetSizeType.Small, Columns = 1, Rows = 1 },
        new WidgetSize { Id = "launcher-medium", Name = "中", Type = WidgetSizeType.Medium, Columns = 2, Rows = 1 }
    };

    public override WidgetSettings CreateDefaultSettings()
    {
        var settings = new WidgetSettings();
        settings.SetValue("target", "https://www.bing.com");
        settings.SetValue("label", "必应");
        return settings;
    }

    public override Control CreateView(WidgetContext context)
    {
        var target = context.Settings.GetValue("target", "https://www.bing.com") ?? "https://www.bing.com";
        var label = context.Settings.GetValue("label", "必应") ?? "必应";

        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(8),
            FontSize = 14
        };

        button.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex, "Failed to launch {Target}", target);
            }
        };

        return button;
    }
}
