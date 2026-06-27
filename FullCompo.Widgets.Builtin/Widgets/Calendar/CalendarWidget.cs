using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FullCompo.Core.Models;
using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

namespace FullCompo.Widgets.Builtin.Widgets.Calendar;

public class CalendarWidget : WidgetBase
{
    public override string Id => "builtin.calendar";
    public override string Name => "日历";
    public override string Description => "显示日期或月视图";

    public override IEnumerable<WidgetSize> SupportedSizes => new[]
    {
        new WidgetSize { Id = "small-square", Name = "小方", Type = WidgetSizeType.Small, Columns = 1, Rows = 1, Width = 80, Height = 80 },
        new WidgetSize { Id = "small-hbar", Name = "小长条", Type = WidgetSizeType.Small, Columns = 2, Rows = 1, Width = 180, Height = 40 },
        new WidgetSize { Id = "medium-hbar", Name = "中横条", Type = WidgetSizeType.Medium, Columns = 2, Rows = 1, Width = 200, Height = 100 },
        new WidgetSize { Id = "medium-square", Name = "中方", Type = WidgetSizeType.Medium, Columns = 2, Rows = 2, Width = 140, Height = 140 },
        new WidgetSize { Id = "large-square", Name = "大方", Type = WidgetSizeType.Large, Columns = 3, Rows = 3, Width = 220, Height = 220 }
    };

    public override WidgetSettings CreateDefaultSettings()
    {
        var settings = new WidgetSettings();
        settings.SetValue("viewMode", "auto");
        settings.SetValue("firstDayOfWeek", (int)DayOfWeek.Sunday);
        return settings;
    }

    public override Control CreateView(WidgetContext context)
    {
        var viewMode = context.Settings.GetValue("viewMode", "auto");
        var firstDay = (DayOfWeek)context.Settings.GetValue("firstDayOfWeek", (int)DayOfWeek.Sunday);

        var foreground = GetThemeBrush("ThemeForegroundBrush");
        var secondaryForeground = GetThemeBrush("ThemeSecondaryForegroundBrush");
        var accent = GetThemeBrush("ThemeAccentBrush");

        var root = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var isMonthView = viewMode == "month" ||
                          (viewMode == "auto" && context.CurrentSize.Rows >= 2 && context.CurrentSize.Columns >= 2);

        if (isMonthView)
        {
            BuildMonthView(root, foreground, secondaryForeground, accent, firstDay);
        }
        else
        {
            BuildDateView(root, foreground, secondaryForeground, accent, context.CurrentSize);
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        timer.Tick += (_, _) =>
        {
            root.Children.Clear();
            if (isMonthView)
                BuildMonthView(root, foreground, secondaryForeground, accent, firstDay);
            else
                BuildDateView(root, foreground, secondaryForeground, accent, context.CurrentSize);
        };
        timer.Start();

        return root;
    }

    public override Control? CreateSettingsView(WidgetSettings settings)
    {
        var viewMode = settings.GetValue("viewMode", "auto");
        var firstDay = settings.GetValue("firstDayOfWeek", (int)DayOfWeek.Sunday);

        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock { Text = "视图模式" });
        var modeBox = new ComboBox();
        var modes = new[] { "auto", "date", "month" };
        foreach (var m in modes) modeBox.Items.Add(m);
        modeBox.SelectedItem = viewMode;
        modeBox.SelectionChanged += (_, _) =>
        {
            if (modeBox.SelectedItem is string selected)
                settings.SetValue("viewMode", selected);
        };
        panel.Children.Add(modeBox);

        panel.Children.Add(new TextBlock { Text = "每周第一天" });
        var dayBox = new ComboBox();
        var days = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday };
        foreach (var d in days) dayBox.Items.Add(d.ToString());
        dayBox.SelectedItem = ((DayOfWeek)firstDay).ToString();
        dayBox.SelectionChanged += (_, _) =>
        {
            if (dayBox.SelectedItem is string selected && Enum.TryParse<DayOfWeek>(selected, out var day))
                settings.SetValue("firstDayOfWeek", (int)day);
        };
        panel.Children.Add(dayBox);

        return panel;
    }

    private static void BuildDateView(Grid root, IBrush foreground, IBrush secondaryForeground, IBrush accent, WidgetSize size)
    {
        var now = DateTime.Now;
        var isWide = size.Width >= size.Height * 2;
        var isLarge = size.Width >= 140;

        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2,
            Orientation = isWide ? Orientation.Horizontal : Orientation.Vertical
        };

        var dayText = new TextBlock
        {
            Text = now.ToString("dd"),
            FontSize = isLarge ? 42 : 22,
            FontWeight = FontWeight.Bold,
            Foreground = accent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var infoPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 0
        };

        var monthText = new TextBlock
        {
            Text = now.ToString("yyyy/MM"),
            FontSize = isLarge ? 14 : 10,
            Foreground = foreground,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var weekText = new TextBlock
        {
            Text = now.ToString("ddd"),
            FontSize = isLarge ? 14 : 10,
            Foreground = secondaryForeground,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        infoPanel.Children.Add(monthText);
        infoPanel.Children.Add(weekText);

        panel.Children.Add(dayText);
        panel.Children.Add(infoPanel);

        root.Children.Add(panel);
    }

    private static void BuildMonthView(Grid root, IBrush foreground, IBrush secondaryForeground, IBrush accent, DayOfWeek firstDay)
    {
        var now = DateTime.Now;
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Spacing = 4,
            Margin = new Thickness(6)
        };

        var header = new TextBlock
        {
            Text = now.ToString("yyyy年M月"),
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = foreground,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(header);

        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        for (int i = 0; i < 7; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int i = 0; i < 7; i++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        var dayNames = new[] { "日", "一", "二", "三", "四", "五", "六" };

        for (int i = 0; i < 7; i++)
        {
            var dayIndex = (i + (int)firstDay) % 7;
            var dayHeader = new TextBlock
            {
                Text = dayNames[dayIndex],
                FontSize = 9,
                Foreground = secondaryForeground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(dayHeader, 0);
            Grid.SetColumn(dayHeader, i);
            grid.Children.Add(dayHeader);
        }

        var firstOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOffset = ((int)firstOfMonth.DayOfWeek - (int)firstDay + 7) % 7;
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var cellRow = (startOffset + day - 1) / 7 + 1;
            var cellCol = (startOffset + day - 1) % 7;
            if (cellRow >= 7) continue;

            var isToday = day == now.Day;
            var dayText = new TextBlock
            {
                Text = day.ToString(),
                FontSize = 10,
                Foreground = isToday ? accent : foreground,
                FontWeight = isToday ? FontWeight.Bold : FontWeight.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (isToday)
            {
                var highlight = new Border
                {
                    Background = accent,
                    CornerRadius = new CornerRadius(10),
                    Width = 18,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = dayText
                };
                Grid.SetRow(highlight, cellRow);
                Grid.SetColumn(highlight, cellCol);
                grid.Children.Add(highlight);
            }
            else
            {
                Grid.SetRow(dayText, cellRow);
                Grid.SetColumn(dayText, cellCol);
                grid.Children.Add(dayText);
            }
        }

        panel.Children.Add(grid);
        root.Children.Add(panel);
    }
}
