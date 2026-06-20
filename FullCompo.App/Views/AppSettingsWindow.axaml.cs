using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FullCompo.App.ViewModels;
using FullCompo.App.Views.SettingPages;
using Microsoft.Extensions.DependencyInjection;

namespace FullCompo.App.Views;

public partial class AppSettingsWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly AppSettingsWindowViewModel _viewModel;

    public AppSettingsWindow()
    {
        _services = null!;
        _viewModel = null!;
        InitializeComponent();
    }

    public AppSettingsWindow(IServiceProvider services)
    {
        _services = services;
        _viewModel = new AppSettingsWindowViewModel(services, Close);
        DataContext = _viewModel;
        InitializeComponent();
        SetupNavigation();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupNavigation()
    {
        var frame = this.FindControl<Frame>("ContentFrame");
        if (frame != null)
        {
            frame.NavigationPageFactory = new SettingsPageFactory(_viewModel);
        }

        var nav = this.FindControl<NavigationView>("Nav");
        if (nav != null)
        {
            foreach (var item in nav.MenuItems)
            {
                if (item is NavigationViewItem { Tag: "General" } general)
                {
                    nav.SelectedItem = general;
                    break;
                }
            }
        }
    }

    private void Nav_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        var container = e.SelectedItemContainer ?? e.SelectedItem as NavigationViewItem;
        if (container?.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "General" => typeof(GeneralSettingsPage),
            "Appearance" => typeof(AppearanceSettingsPage),
            "Weather" => typeof(WeatherSettingsPage),
            "About" => typeof(AboutSettingsPage),
            _ => typeof(GeneralSettingsPage)
        };

        var frame = this.FindControl<Frame>("ContentFrame");
        frame?.Navigate(pageType);
    }
}
