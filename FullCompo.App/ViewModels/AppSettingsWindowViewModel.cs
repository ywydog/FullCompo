using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.ViewModels;

public sealed class AppSettingsWindowViewModel : INotifyPropertyChanged
{
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;
    private readonly IWeatherService _weatherService;
    private readonly Action _close;

    private ThemeConfig? _selectedTheme;

    public AppSettingsWindowViewModel(IServiceProvider services, Action close)
    {
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();
        _weatherService = services.GetRequiredService<IWeatherService>();
        _close = close;

        Settings = _configService.AppSettings;
        AvailableThemes = _themeService.AvailableThemes;
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Id == Settings.ThemeId)
                        ?? AvailableThemes.FirstOrDefault();

        Languages = new[] { "zh-CN", "en-US" };
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "未知版本";

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    public AppSettings Settings { get; }

    public IReadOnlyList<ThemeConfig> AvailableThemes { get; }

    public string Version { get; }

    public ThemeConfig? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme != value)
            {
                _selectedTheme = value;
                OnPropertyChanged();
            }
        }
    }

    public string[] Languages { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void Save()
    {
        if (SelectedTheme != null)
        {
            Settings.ThemeId = SelectedTheme.Id;
        }

        _configService.Save();
        _themeService.ApplyTheme(Settings.ThemeId);
        _weatherService.UpdateInterval();
        _ = _weatherService.RefreshAsync();

        _close();
    }

    private void Cancel() => _close();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
