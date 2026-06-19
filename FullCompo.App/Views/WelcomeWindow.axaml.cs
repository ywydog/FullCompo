using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Ellipse = Avalonia.Controls.Shapes.Ellipse;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.Views;

public partial class WelcomeWindow : Window
{
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;
    private int _currentStep = 1;

    private readonly List<StackPanel> _stepContents = new();
    private readonly List<StackPanel> _stepIndicators = new();

    public WelcomeWindow()
    {
        _configService = null!;
        _themeService = null!;
        InitializeComponent();
    }

    public WelcomeWindow(IServiceProvider services)
    {
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();
        InitializeComponent();
        InitializeSteps();
        BindControls();
        UpdateStep();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeSteps()
    {
        _stepContents.Add(this.FindControl<StackPanel>("Step1Content")!);
        _stepContents.Add(this.FindControl<StackPanel>("Step2Content")!);
        _stepContents.Add(this.FindControl<StackPanel>("Step3Content")!);
        _stepContents.Add(this.FindControl<StackPanel>("Step4Content")!);

        _stepIndicators.Add(this.FindControl<StackPanel>("Step1Indicator")!);
        _stepIndicators.Add(this.FindControl<StackPanel>("Step2Indicator")!);
        _stepIndicators.Add(this.FindControl<StackPanel>("Step3Indicator")!);
        _stepIndicators.Add(this.FindControl<StackPanel>("Step4Indicator")!);
    }

    private void BindControls()
    {
        var themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
        var startupCheckBox = this.FindControl<CheckBox>("StartupCheckBox");
        var shortcutCheckBox = this.FindControl<CheckBox>("ShortcutCheckBox");
        var backButton = this.FindControl<Button>("BackButton");
        var nextButton = this.FindControl<Button>("NextButton");
        var finishButton = this.FindControl<Button>("FinishButton");

        if (themeComboBox != null)
        {
            themeComboBox.ItemsSource = _themeService.AvailableThemes.Select(t => t.Name).ToList();
            var currentTheme = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == _configService.AppSettings.ThemeId)
                ?? _themeService.AvailableThemes.FirstOrDefault();
            themeComboBox.SelectedItem = currentTheme?.Name;
            themeComboBox.SelectionChanged += (_, _) =>
            {
                if (themeComboBox.SelectedItem is string themeName)
                {
                    var theme = _themeService.AvailableThemes.FirstOrDefault(t => t.Name == themeName);
                    if (theme != null)
                    {
                        _themeService.ApplyTheme(theme.Id);
                        _configService.AppSettings.ThemeId = theme.Id;
                        UpdateThemePreview(theme);
                    }
                }
            };
        }

        if (startupCheckBox != null)
        {
            startupCheckBox.IsChecked = _configService.AppSettings.RunOnStartup;
        }

        if (shortcutCheckBox != null)
        {
            shortcutCheckBox.IsChecked = false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shortcutCheckBox.IsVisible = false;
            }
        }

        if (backButton != null) backButton.Click += (_, _) => GoToStep(_currentStep - 1);
        if (nextButton != null) nextButton.Click += (_, _) => GoToStep(_currentStep + 1);
        if (finishButton != null) finishButton.Click += (_, _) => Finish();

        var initialTheme = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == _configService.AppSettings.ThemeId)
            ?? _themeService.AvailableThemes.FirstOrDefault();
        if (initialTheme != null)
        {
            UpdateThemePreview(initialTheme);
        }
    }

    private void GoToStep(int step)
    {
        if (step < 1 || step > 4) return;
        _currentStep = step;
        UpdateStep();
    }

    private void UpdateStep()
    {
        for (var i = 0; i < _stepContents.Count; i++)
        {
            _stepContents[i].IsVisible = i + 1 == _currentStep;
        }

        for (var i = 0; i < _stepIndicators.Count; i++)
        {
            var indicator = _stepIndicators[i];
            var ellipse = indicator.Children[0] as Ellipse;
            var text = indicator.Children[1] as TextBlock;

            if (ellipse == null || text == null) continue;

            if (i + 1 == _currentStep)
            {
                ellipse.Fill = new SolidColorBrush(Color.Parse("#FF0078D4"));
                text.Foreground = new SolidColorBrush(Color.Parse("#FF1F1F1F"));
            }
            else if (i + 1 < _currentStep)
            {
                ellipse.Fill = new SolidColorBrush(Color.Parse("#FF0078D4"));
                text.Foreground = new SolidColorBrush(Color.Parse("#FF5F5F5F"));
            }
            else
            {
                ellipse.Fill = new SolidColorBrush(Color.Parse("#FFC4C4C4"));
                text.Foreground = new SolidColorBrush(Color.Parse("#FF8A8A8A"));
            }
        }

        var backButton = this.FindControl<Button>("BackButton");
        var nextButton = this.FindControl<Button>("NextButton");
        var finishButton = this.FindControl<Button>("FinishButton");

        if (backButton != null) backButton.IsVisible = _currentStep > 1;
        if (nextButton != null) nextButton.IsVisible = _currentStep < 4;
        if (finishButton != null) finishButton.IsVisible = _currentStep == 4;
    }

    private void UpdateThemePreview(ThemeConfig theme)
    {
        var preview = this.FindControl<Border>("ThemePreview");
        var previewText = this.FindControl<TextBlock>("PreviewTextBlock");
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

    private void Finish()
    {
        var startupCheckBox = this.FindControl<CheckBox>("StartupCheckBox");
        var shortcutCheckBox = this.FindControl<CheckBox>("ShortcutCheckBox");

        if (startupCheckBox != null)
        {
            _configService.AppSettings.RunOnStartup = startupCheckBox.IsChecked ?? false;
        }

        if (shortcutCheckBox != null && shortcutCheckBox.IsChecked == true)
        {
            TryCreateDesktopShortcut();
        }

        _configService.AppSettings.IsFirstRun = false;
        _configService.Save();
        Close();
    }

    private static void TryCreateDesktopShortcut()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executablePath)) return;

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktopPath, "FullCompo.lnk");

            // Use Windows Script Host to create shortcut
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type == null) return;

            dynamic shell = Activator.CreateInstance(type)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = executablePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath);
            shortcut.Description = "启动 FullCompo";
            shortcut.Save();
        }
        catch
        {
            // Ignore shortcut creation failure
        }
    }
}
