using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FullCompo.App.Views.SettingPages;

public partial class WeatherSettingsPage : UserControl
{
    public WeatherSettingsPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
