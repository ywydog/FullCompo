using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.Views;

public partial class AppSettingsWindow : Window
{
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;

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
        BindControls();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BindControls()
    {
        var themeBox = this.FindControl<ComboBox>("ThemeBox");
        var startupToggle = this.FindControl<ToggleSwitch>("StartupToggle");
        var trayToggle = this.FindControl<ToggleSwitch>("TrayToggle");
        var clickThroughToggle = this.FindControl<ToggleSwitch>("ClickThroughToggle");
        var spacingSlider = this.FindControl<Slider>("SpacingSlider");
        var spacingValueText = this.FindControl<TextBlock>("SpacingValueText");
        var saveButton = this.FindControl<Button>("SaveButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (themeBox != null)
        {
            themeBox.ItemsSource = _themeService.AvailableThemes.Select(t => t.Name).ToList();
            themeBox.SelectedItem = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == _configService.AppSettings.ThemeId)?.Name;
        }
        if (startupToggle != null) startupToggle.IsChecked = _configService.AppSettings.RunOnStartup;
        if (trayToggle != null) trayToggle.IsChecked = _configService.AppSettings.ShowTrayIcon;
        if (clickThroughToggle != null) clickThroughToggle.IsChecked = _configService.AppSettings.ClickThrough;

        if (spacingSlider != null)
        {
            spacingSlider.Value = _configService.AppSettings.WidgetSpacing;
            spacingSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property == Slider.ValueProperty && spacingValueText != null)
                {
                    spacingValueText.Text = $"当前: {spacingSlider.Value:F1}（约 {spacingSlider.Value * 16:F0}px）";
                }
            };
        }
        if (spacingValueText != null)
        {
            spacingValueText.Text = $"当前: {_configService.AppSettings.WidgetSpacing:F1}（约 {_configService.AppSettings.WidgetSpacing * 16:F0}px）";
        }

        if (saveButton != null) saveButton.Click += (_, _) => Save();
        if (cancelButton != null) cancelButton.Click += (_, _) => Close();
    }

    private void Save()
    {
        var themeBox = this.FindControl<ComboBox>("ThemeBox");
        var startupToggle = this.FindControl<ToggleSwitch>("StartupToggle");
        var trayToggle = this.FindControl<ToggleSwitch>("TrayToggle");
        var clickThroughToggle = this.FindControl<ToggleSwitch>("ClickThroughToggle");
        var spacingSlider = this.FindControl<Slider>("SpacingSlider");

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
        if (spacingSlider != null) _configService.AppSettings.WidgetSpacing = spacingSlider.Value;

        _configService.Save();
        Close();
    }
}
