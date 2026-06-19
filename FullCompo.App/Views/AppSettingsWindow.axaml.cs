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
    private readonly IWeatherService _weatherService;

    private readonly List<ScrollViewer> _pages = new();
    private readonly List<CitySearchResult> _citySearchResults = new();

    public AppSettingsWindow()
    {
        // Design-time constructor
        _configService = null!;
        _themeService = null!;
        _weatherService = null!;
        InitializeComponent();
    }

    public AppSettingsWindow(IServiceProvider services)
    {
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();
        _weatherService = services.GetRequiredService<IWeatherService>();
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
        _pages.Add(this.FindControl<ScrollViewer>("PageWeather")!);
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
        var dockPositionBox = this.FindControl<ComboBox>("DockPositionBox");
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

        if (dockPositionBox != null)
        {
            dockPositionBox.SelectedIndex = DockPositionToIndex(_configService.AppSettings.DockPosition);
        }

        if (startupToggle != null) startupToggle.IsChecked = _configService.AppSettings.RunOnStartup;
        if (trayToggle != null) trayToggle.IsChecked = _configService.AppSettings.ShowTrayIcon;
        if (clickThroughToggle != null) clickThroughToggle.IsChecked = _configService.AppSettings.ClickThrough;

        if (saveButton != null) saveButton.Click += (_, _) => Save();
        if (cancelButton != null) cancelButton.Click += (_, _) => Close();

        BindWeatherControls();
    }

    private void BindWeatherControls()
    {
        var enabledToggle = this.FindControl<ToggleSwitch>("WeatherEnabledToggle");
        var noTlsToggle = this.FindControl<ToggleSwitch>("WeatherNoTLSToggle");
        var cityText = this.FindControl<TextBlock>("WeatherCityText");
        var searchText = this.FindControl<TextBox>("WeatherCitySearchText");
        var searchButton = this.FindControl<Button>("WeatherSearchButton");
        var cityResults = this.FindControl<ListBox>("WeatherCityResults");

        if (enabledToggle != null) enabledToggle.IsChecked = _configService.AppSettings.WeatherEnabled;
        if (noTlsToggle != null) noTlsToggle.IsChecked = _configService.AppSettings.NoTLSWeatherRequests;
        if (cityText != null) cityText.Text = $"{_configService.AppSettings.WeatherCityName} ({_configService.AppSettings.WeatherCityId})";

        if (searchButton != null && searchText != null && cityResults != null)
        {
            async void OnSearch(object? s, EventArgs e)
            {
                searchButton.IsEnabled = false;
                try
                {
                    _citySearchResults.Clear();
                    var results = await _weatherService.SearchCityAsync(searchText.Text ?? "");
                    _citySearchResults.AddRange(results);
                    cityResults.ItemsSource = results.Select(r => $"{r.Name} ({r.Province} {r.District})").ToList();
                }
                finally
                {
                    searchButton.IsEnabled = true;
                }
            }

            searchButton.Click += OnSearch;
            searchText.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter) OnSearch(s, e);
            };

            cityResults.SelectionChanged += (s, e) =>
            {
                if (cityResults.SelectedIndex >= 0 && cityResults.SelectedIndex < _citySearchResults.Count)
                {
                    var selected = _citySearchResults[cityResults.SelectedIndex];
                    _configService.AppSettings.WeatherCityId = selected.CityId;
                    _configService.AppSettings.WeatherCityName = selected.Name;
                    _configService.AppSettings.WeatherLongitude = selected.Longitude;
                    _configService.AppSettings.WeatherLatitude = selected.Latitude;
                    if (cityText != null) cityText.Text = $"{selected.Name} ({selected.CityId})";
                }
            };
        }
    }

    private static int DockPositionToIndex(string position)
    {
        return position switch
        {
            "free" => 0,
            "top" => 1,
            "top-left" => 2,
            "top-right" => 3,
            "bottom" => 4,
            "bottom-left" => 5,
            "bottom-right" => 6,
            "left" => 7,
            "right" => 8,
            _ => 3
        };
    }

    private static string IndexToDockPosition(int index)
    {
        return index switch
        {
            0 => "free",
            1 => "top",
            2 => "top-left",
            3 => "top-right",
            4 => "bottom",
            5 => "bottom-left",
            6 => "bottom-right",
            7 => "left",
            8 => "right",
            _ => "top-right"
        };
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
        var dockPositionBox = this.FindControl<ComboBox>("DockPositionBox");
        var startupToggle = this.FindControl<ToggleSwitch>("StartupToggle");
        var trayToggle = this.FindControl<ToggleSwitch>("TrayToggle");
        var clickThroughToggle = this.FindControl<ToggleSwitch>("ClickThroughToggle");

        if (dockPositionBox != null)
        {
            _configService.AppSettings.DockPosition = IndexToDockPosition(dockPositionBox.SelectedIndex);
        }

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

        var weatherEnabledToggle = this.FindControl<ToggleSwitch>("WeatherEnabledToggle");
        var weatherNoTlsToggle = this.FindControl<ToggleSwitch>("WeatherNoTLSToggle");
        if (weatherEnabledToggle != null) _configService.AppSettings.WeatherEnabled = weatherEnabledToggle.IsChecked ?? true;
        if (weatherNoTlsToggle != null) _configService.AppSettings.NoTLSWeatherRequests = weatherNoTlsToggle.IsChecked ?? false;

        _configService.Save();
        _ = _weatherService.RefreshAsync();
        Close();
    }
}
