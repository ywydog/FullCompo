using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    public override string Description => "显示当前时间的模拟时钟";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        WidgetSizePresets.Square,
        WidgetSizePresets.LargeSquare
    };

    public override Control CreateView(WidgetContext context)
    {
        const double size = 100;
        const double center = size / 2;

        var canvas = new Canvas
        {
            Width = size,
            Height = size,
            Background = Brushes.Transparent
        };

        var foreground = GetForegroundBrush();

        // 外圈
        var border = new Ellipse
        {
            Width = size,
            Height = size,
            Stroke = foreground,
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(border, 0);
        Canvas.SetTop(border, 0);
        canvas.Children.Add(border);

        // 数字 1-12
        for (var i = 1; i <= 12; i++)
        {
            var angle = i * 30.0 * Math.PI / 180.0;
            var radius = center - 16;
            var x = center + radius * Math.Sin(angle);
            var y = center - radius * Math.Cos(angle);

            var number = new TextBlock
            {
                Text = i.ToString(),
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 居中文字
            var textSize = 12;
            Canvas.SetLeft(number, x - textSize / 2);
            Canvas.SetTop(number, y - textSize / 2);
            canvas.Children.Add(number);
        }

        // 指针
        var hourHand = CreateHand(foreground, 30, 3, center);
        var minuteHand = CreateHand(foreground, 42, 2, center);
        var secondHand = CreateHand(new SolidColorBrush(Colors.Red), 44, 1, center);

        canvas.Children.Add(hourHand);
        canvas.Children.Add(minuteHand);
        canvas.Children.Add(secondHand);

        var updateAction = () =>
        {
            var now = DateTime.Now;
            var secondAngle = now.Second * 6.0;
            var minuteAngle = now.Minute * 6.0 + now.Second * 0.1;
            var hourAngle = (now.Hour % 12) * 30.0 + now.Minute * 0.5;

            SetHandAngle(hourHand, hourAngle, center);
            SetHandAngle(minuteHand, minuteAngle, center);
            SetHandAngle(secondHand, secondAngle, center);
        };

        updateAction();

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => updateAction();
        timer.Start();

        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = canvas
        };
    }

    private static Line CreateHand(IBrush brush, double length, double thickness, double center)
    {
        return new Line
        {
            StartPoint = new Point(center, center),
            EndPoint = new Point(center, center - length),
            Stroke = brush,
            StrokeThickness = thickness,
            StrokeLineCap = PenLineCap.Round,
            RenderTransformOrigin = new RelativePoint(center / 100.0, center / 100.0, RelativeUnit.Relative)
        };
    }

    private static void SetHandAngle(Line hand, double angle, double center)
    {
        hand.RenderTransform = new RotateTransform(angle, center, center);
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
