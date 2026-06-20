using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.Views;

public partial class AppSettingsWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;
    private readonly IWeatherService _weatherService;

    private ComboBox _languageBox = null!;
    private ComboBox _themeBox = null!;
    private ToggleSwitch _startupToggle = null!;
    private ToggleSwitch _trayToggle = null!;
    private ToggleSwitch _clickThroughToggle = null!;
    private TextBox _shortcutBox = null!;
    private Slider _spacingSlider = null!;
    private TextBlock _spacingValueText = null!;
    private TextBox _cityBox = null!;
    private Slider _refreshSlider = null!;
    private TextBlock _refreshValueText = null!;

    private Control? _generalPage;
    private Control? _appearancePage;
    private Control? _weatherPage;
    private Control? _aboutPage;

    public AppSettingsWindow()
    {
        _services = null!;
        _configService = null!;
        _themeService = null!;
        _weatherService = null!;
        InitializeComponent();
    }

    public AppSettingsWindow(IServiceProvider services)
    {
        _services = services;
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();
        _weatherService = services.GetRequiredService<IWeatherService>();
        InitializeComponent();
        BuildPages();
        BindControls();
        SetupEvents();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildPages()
    {
        _generalPage = BuildGeneralPage();
        _appearancePage = BuildAppearancePage();
        _weatherPage = BuildWeatherPage();
        _aboutPage = BuildAboutPage();

        var navList = this.FindControl<ListBox>("NavList");
        var contentHost = this.FindControl<TransitioningContentControl>("ContentHost");
        if (navList != null)
        {
            navList.SelectedIndex = 0;
        }
        if (contentHost != null)
        {
            contentHost.Content = _generalPage;
        }
    }

    private void BindControls()
    {
        var settings = _configService.AppSettings;

        _languageBox.ItemsSource = new[] { "zh-CN", "en-US" };
        _languageBox.SelectedItem = settings.Language;

        _themeBox.ItemsSource = _themeService.AvailableThemes.Select(t => t.Name).ToList();
        _themeBox.SelectedItem = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == settings.ThemeId)?.Name;

        _startupToggle.IsChecked = settings.RunOnStartup;
        _trayToggle.IsChecked = settings.ShowTrayIcon;
        _clickThroughToggle.IsChecked = settings.ClickThrough;
        _shortcutBox.Text = settings.EditModeShortcut;

        _spacingSlider.Value = settings.WidgetSpacing;
        _spacingValueText.Text = FormatSpacing(settings.WidgetSpacing);

        _cityBox.Text = settings.WeatherCity;
        _refreshSlider.Value = settings.WeatherRefreshIntervalMinutes;
        _refreshValueText.Text = FormatRefreshInterval(settings.WeatherRefreshIntervalMinutes);
    }

    private void SetupEvents()
    {
        var navList = this.FindControl<ListBox>("NavList");
        var contentHost = this.FindControl<TransitioningContentControl>("ContentHost");
        var saveButton = this.FindControl<Button>("SaveButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (navList != null && contentHost != null)
        {
            navList.SelectionChanged += (_, _) =>
            {
                contentHost.Content = navList.SelectedIndex switch
                {
                    0 => _generalPage,
                    1 => _appearancePage,
                    2 => _weatherPage,
                    3 => _aboutPage,
                    _ => _generalPage
                };
            };
        }

        if (saveButton != null) saveButton.Click += (_, _) => Save();
        if (cancelButton != null) cancelButton.Click += (_, _) => Close();

        _spacingSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                _spacingValueText.Text = FormatSpacing(_spacingSlider.Value);
            }
        };

        _refreshSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                _refreshValueText.Text = FormatRefreshInterval((int)_refreshSlider.Value);
            }
        };
    }

    private Control BuildGeneralPage()
    {
        _languageBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        _startupToggle = new ToggleSwitch();
        _trayToggle = new ToggleSwitch();
        _clickThroughToggle = new ToggleSwitch();
        _shortcutBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, Watermark = "例如 Ctrl+Shift+E" };

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    CreateHeader("常规设置"),
                    CreateLabel("语言"),
                    _languageBox,
                    CreateLabel("开机自启"),
                    _startupToggle,
                    CreateLabel("显示托盘图标"),
                    _trayToggle,
                    CreateLabel("穿透点击（运行时鼠标可穿透面板）"),
                    _clickThroughToggle,
                    CreateLabel("编辑模式快捷键"),
                    _shortcutBox
                }
            }
        };
    }

    private Control BuildAppearancePage()
    {
        _themeBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        _spacingSlider = new Slider
        {
            Minimum = 0,
            Maximum = 2,
            TickFrequency = 0.1,
            IsSnapToTickEnabled = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _spacingValueText = new TextBlock { Foreground = Brushes.Gray, FontSize = 12 };

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    CreateHeader("外观设置"),
                    CreateLabel("主题"),
                    _themeBox,
                    CreateLabel("组件间距"),
                    _spacingSlider,
                    _spacingValueText
                }
            }
        };
    }

    private Control BuildWeatherPage()
    {
        _cityBox = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, Watermark = "例如 北京" };
        _refreshSlider = new Slider
        {
            Minimum = 1,
            Maximum = 3,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _refreshValueText = new TextBlock { Foreground = Brushes.Gray, FontSize = 12 };

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    CreateHeader("天气设置"),
                    CreateLabel("城市"),
                    _cityBox,
                    CreateLabel("刷新间隔"),
                    _refreshSlider,
                    _refreshValueText
                }
            }
        };
    }

    private Control BuildAboutPage()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "未知版本";

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    CreateHeader("关于"),
                    new TextBlock
                    {
                        Text = "全面组件",
                        FontSize = 22,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse("#1A1A1A"))
                    },
                    new TextBlock
                    {
                        Text = $"版本 {version}",
                        Foreground = Brushes.Gray,
                        FontSize = 13
                    },
                    new TextBlock
                    {
                        Text = "一款简洁的桌面组件工具。",
                        Foreground = new SolidColorBrush(Color.Parse("#333333")),
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private static TextBlock CreateHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#1A1A1A")),
            Margin = new Thickness(0, 0, 0, 12)
        };
    }

    private static TextBlock CreateLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#333333")),
            Margin = new Thickness(0, 12, 0, 4)
        };
    }

    private static string FormatSpacing(double value)
    {
        return $"当前: {value:F1}（约 {value * 16:F0}px）";
    }

    private static string FormatRefreshInterval(int minutes)
    {
        return $"当前: {minutes} 分钟";
    }

    private void Save()
    {
        var settings = _configService.AppSettings;

        if (_languageBox.SelectedItem is string language)
        {
            settings.Language = language;
        }

        if (_themeBox.SelectedItem is string themeName)
        {
            var theme = _themeService.AvailableThemes.FirstOrDefault(t => t.Name == themeName);
            if (theme != null)
            {
                settings.ThemeId = theme.Id;
                _themeService.ApplyTheme(theme.Id);
            }
        }

        settings.RunOnStartup = _startupToggle.IsChecked ?? false;
        settings.ShowTrayIcon = _trayToggle.IsChecked ?? false;
        settings.ClickThrough = _clickThroughToggle.IsChecked ?? false;
        settings.EditModeShortcut = _shortcutBox.Text ?? "Ctrl+Shift+E";
        settings.WidgetSpacing = _spacingSlider.Value;

        settings.WeatherCity = string.IsNullOrWhiteSpace(_cityBox.Text) ? "北京" : _cityBox.Text.Trim();
        settings.WeatherRefreshIntervalMinutes = Math.Clamp((int)_refreshSlider.Value, 1, 3);

        _configService.Save();
        _weatherService.UpdateInterval();
        _ = _weatherService.RefreshAsync();

        Close();
    }
}
