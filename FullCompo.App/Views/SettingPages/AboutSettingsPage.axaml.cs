using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FullCompo.App.Views.SettingPages;

public partial class AboutSettingsPage : UserControl
{
    public AboutSettingsPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
