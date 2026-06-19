using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.Views;

public partial class AppSettingsWindow : Window
{
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;

    private readonly List<ScrollViewer> _pages = new();

    public AppSettingsWindow()
    {
        // Design-time constructor
        _configService = null!;
        _themeService = null!;
        InitializeComponent();
    }

    public AppSettingsWindow(IServiceProvider services)
    {
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();
        InitializeComponent();
        InitializePages();
        BindControls();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializePages()
    {
        _pages.Add(this.FindControl<ScrollViewer>("PageGeneral")!);
        _pages.Add(this.FindControl<ScrollViewer>("PageAppearance")!);
        _pages.Add(this.FindControl<ScrollViewer>("PagePanel")!);
        _pages.Add(this.FindControl<ScrollViewer>("PageAbout")!);

        var navList = this.FindControl<ListBox>("NavList");
        if (navList != null)
        {
            navList.SelectedIndex = 0;
            navList.SelectionChanged += OnNavSelectionChanged;
        }

        // Show first page
        ShowPage(0);
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var navList = this.FindControl<ListBox>("NavList");
        if (navList?.SelectedIndex >= 0)
        {
            ShowPage(navList.SelectedIndex);
        }
    }

    private void ShowPage(int index)
    {
        for (var i = 0; i < _pages.Count; i++)
        {
            if (_pages[i] != null)
            {
                _pages[i].IsVisible = i == index;
            }
        }
    }

    private void BindControls()
    {
        var themeBox = this.FindControl<ComboBox>("ThemeBox");
        var startupToggle = this.FindControl<ToggleSwitch>("StartupToggle");
        var trayToggle = this.FindControl<ToggleSwitch>("TrayToggle");
        var clickThroughToggle = this.FindControl<ToggleSwitch>("ClickThroughToggle");
        var saveButton = this.FindControl<Button>("SaveButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (themeBox != null)
        {
            themeBox.ItemsSource = _themeService.AvailableThemes.Select(t => t.Name).ToList();
            var currentTheme = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == _configService.AppSettings.ThemeId)
                ?? _themeService.AvailableThemes.FirstOrDefault();
            themeBox.SelectedItem = currentTheme?.Name;
            themeBox.SelectionChanged += (_, _) =>
            {
                if (themeBox.SelectedItem is string themeName)
                {
                    var theme = _themeService.AvailableThemes.FirstOrDefault(t => t.Name == themeName);
                    if (theme != null)
                    {
                        UpdateThemePreview(theme);
                    }
                }
            };

            // Show initial preview
            if (currentTheme != null)
            {
                UpdateThemePreview(currentTheme);
            }
        }

        if (startupToggle != null) startupToggle.IsChecked = _configService.AppSettings.RunOnStartup;
        if (trayToggle != null) trayToggle.IsChecked = _configService.AppSettings.ShowTrayIcon;
        if (clickThroughToggle != null) clickThroughToggle.IsChecked = _configService.AppSettings.ClickThrough;

        if (saveButton != null) saveButton.Click += (_, _) => Save();
        if (cancelButton != null) cancelButton.Click += (_, _) => Close();
    }

    private void UpdateThemePreview(ThemeConfig theme)
    {
        var preview = this.FindControl<Border>("ThemePreviewSettings");
        var previewText = this.FindControl<TextBlock>("PreviewTextBlockSettings");
        if (preview != null)
        {
            preview.Background = new SolidColorBrush(theme.BackgroundColor);
            preview.BorderBrush = new SolidColorBrush(theme.BorderColor);
        }
        if (previewText != null)
        {
            previewText.Foreground = new SolidColorBrush(theme.ForegroundColor);
        }
    }

    private void Save()
    {
        var themeBox = this.FindControl<ComboBox>("ThemeBox");
        var startupToggle = this.FindControl<ToggleSwitch>("StartupToggle");
        var trayToggle = this.FindControl<ToggleSwitch>("TrayToggle");
        var clickThroughToggle = this.FindControl<ToggleSwitch>("ClickThroughToggle");

        if (themeBox?.SelectedItem is string themeName)
        {
            var theme = _themeService.AvailableThemes.FirstOrDefault(t => t.Name == themeName);
            if (theme != null)
            {
                _configService.AppSettings.ThemeId = theme.Id;
                _themeService.ApplyTheme(theme.Id);
            }
        }

        if (startupToggle != null) _configService.AppSettings.RunOnStartup = startupToggle.IsChecked ?? false;
        if (trayToggle != null) _configService.AppSettings.ShowTrayIcon = trayToggle.IsChecked ?? false;
        if (clickThroughToggle != null) _configService.AppSettings.ClickThrough = clickThroughToggle.IsChecked ?? false;

        _configService.Save();
        Close();
    }
}
