using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FullCompo.App.Helpers;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.Views;

public partial class WelcomeWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;

    private readonly List<(string Title, Func<Control> Build)> _steps = new();
    private int _currentStep;

    private readonly RadioButton _lightRadio;
    private readonly RadioButton _darkRadio;
    private readonly RadioButton _glassRadio;
    private readonly ToggleSwitch _startupToggle;
    private readonly ToggleSwitch _trayToggle;
    private readonly ToggleSwitch _clickThroughToggle;

    public event EventHandler? Completed;

    public WelcomeWindow(IServiceProvider services)
    {
        _services = services;
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();

        InitializeComponent();
        LoadLogo();

        _lightRadio = new RadioButton { Content = "浅色", Foreground = new SolidColorBrush(Color.Parse("#1A1A1A")) };
        _darkRadio = new RadioButton { Content = "深色", Foreground = new SolidColorBrush(Color.Parse("#1A1A1A")) };
        _glassRadio = new RadioButton { Content = "毛玻璃", Foreground = new SolidColorBrush(Color.Parse("#1A1A1A")) };
        _startupToggle = new ToggleSwitch { OffContent = "", OnContent = "" };
        _trayToggle = new ToggleSwitch { OffContent = "", OnContent = "" };
        _clickThroughToggle = new ToggleSwitch { OffContent = "", OnContent = "" };

        InitializeSteps();
        BuildStepList();
        BindButtons();
        ShowStep(0);
    }

    private void InitializeSteps()
    {
        _steps.Add(("欢迎", BuildWelcomeStep));
        _steps.Add(("外观", BuildAppearanceStep));
        _steps.Add(("快捷设置", BuildQuickSettingsStep));
        _steps.Add(("完成", BuildFinishStep));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BindButtons()
    {
        var backButton = this.FindControl<Button>("BackButton");
        var nextButton = this.FindControl<Button>("NextButton");

        if (backButton != null) backButton.Click += (_, _) => GoBack();
        if (nextButton != null) nextButton.Click += (_, _) => GoNext();
    }

    private void BuildStepList()
    {
        var stepList = this.FindControl<StackPanel>("StepList");
        if (stepList == null) return;

        stepList.Children.Clear();
        for (var i = 0; i < _steps.Count; i++)
        {
            var index = i;
            var text = new TextBlock
            {
                Text = $"{(i == _currentStep ? "●" : "○")} {_steps[i].Title}",
                FontSize = 13,
                Foreground = new SolidColorBrush(i == _currentStep ? Color.Parse("#0078D4") : Color.Parse("#666666"))
            };
            stepList.Children.Add(text);
        }
    }

    private void ShowStep(int index)
    {
        _currentStep = index;

        var content = this.FindControl<ContentControl>("StepContent");
        if (content != null)
        {
            content.Content = _steps[index].Build();
        }

        var backButton = this.FindControl<Button>("BackButton");
        var nextButton = this.FindControl<Button>("NextButton");

        if (backButton != null) backButton.IsVisible = index > 0;
        if (nextButton != null)
        {
            nextButton.Content = index == _steps.Count - 1 ? "完成" : "下一步";
            nextButton.IsVisible = true;
        }

        BuildStepList();
    }

    private void GoBack()
    {
        if (_currentStep > 0)
        {
            ShowStep(_currentStep - 1);
        }
    }

    private void GoNext()
    {
        if (_currentStep < _steps.Count - 1)
        {
            ShowStep(_currentStep + 1);
        }
        else
        {
            SaveSettings();
            Completed?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var themeId = _themeService.AvailableThemes.First().Id;
            if (_lightRadio.IsChecked == true) themeId = "light";
            else if (_darkRadio.IsChecked == true) themeId = "dark";
            else if (_glassRadio.IsChecked == true) themeId = "glass";

            _configService.AppSettings.ThemeId = themeId;
            _themeService.ApplyTheme(themeId);

            _configService.AppSettings.RunOnStartup = _startupToggle.IsChecked ?? false;
            _configService.AppSettings.ShowTrayIcon = _trayToggle.IsChecked ?? false;
            _configService.AppSettings.ClickThrough = _clickThroughToggle.IsChecked ?? false;
            _configService.AppSettings.IsFirstRun = false;

            _configService.Save();
        }
        catch (Exception ex)
        {
            AppLog.WriteException("WelcomeWindow.SaveSettings", ex);
        }
    }

    private Control BuildWelcomeStep()
    {
        return new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "欢迎使用 FullCompo",
                    FontSize = 24,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#1A1A1A"))
                },
                new TextBlock
                {
                    Text = "在桌面上搭建你的个性化信息面板。\n首次使用，让我们花一分钟完成基本设置。",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.Parse("#333333")),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
    }

    private Control BuildAppearanceStep()
    {
        var selectedTheme = _themeService.AvailableThemes.FirstOrDefault(t => t.Id == _configService.AppSettings.ThemeId)?.Id ?? "dark";
        _lightRadio.IsChecked = selectedTheme == "light";
        _darkRadio.IsChecked = selectedTheme == "dark";
        _glassRadio.IsChecked = selectedTheme == "glass";

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "选择外观",
                    FontSize = 20,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#1A1A1A"))
                },
                new TextBlock
                {
                    Text = "选择你喜欢的主题风格，可以随时在设置中更改。",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.Parse("#333333")),
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel
                {
                    Spacing = 10,
                    Margin = new Thickness(0, 8, 0, 0),
                    Children = { _lightRadio, _darkRadio, _glassRadio }
                }
            }
        };
    }

    private Control BuildQuickSettingsStep()
    {
        _startupToggle.IsChecked = _configService.AppSettings.RunOnStartup;
        _trayToggle.IsChecked = _configService.AppSettings.ShowTrayIcon;
        _clickThroughToggle.IsChecked = _configService.AppSettings.ClickThrough;

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "快捷设置",
                    FontSize = 20,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#1A1A1A"))
                },
                new TextBlock
                {
                    Text = "配置一些常用选项，让 FullCompo 更方便使用。",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.Parse("#333333")),
                    TextWrapping = TextWrapping.Wrap
                },
                BuildToggleRow("开机自动启动 FullCompo", _startupToggle),
                BuildToggleRow("显示托盘图标", _trayToggle),
                BuildToggleRow("鼠标穿透面板（可点击桌面）", _clickThroughToggle)
            }
        };
    }

    private static Control BuildToggleRow(string label, ToggleSwitch toggle)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#1A1A1A"))
        };
        Grid.SetColumn(text, 0);
        Grid.SetColumn(toggle, 1);

        grid.Children.Add(text);
        grid.Children.Add(toggle);

        return grid;
    }

    private Control BuildFinishStep()
    {
        return new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "设置完成",
                    FontSize = 20,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#1A1A1A"))
                },
                new TextBlock
                {
                    Text = "点击“完成”开始使用 FullCompo。\n之后可以在托盘菜单或右键面板进入设置。",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.Parse("#333333")),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
    }

    private void LoadLogo()
    {
        try
        {
            var image = this.FindControl<Image>("LogoImage");
            if (image == null) return;

            Bitmap? bitmap = null;
            try
            {
                using var stream = AssetLoader.Open(new Uri("avares://FullCompo.App/Assets/logo.png"));
                bitmap = new Bitmap(stream);
            }
            catch
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(path))
                {
                    bitmap = new Bitmap(path);
                }
            }

            image.Source = bitmap;
        }
        catch (Exception ex)
        {
            AppLog.WriteException("WelcomeWindow.LoadLogo", ex);
        }
    }
}
