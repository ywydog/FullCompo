using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FullCompo.App.Views.SettingPages;

public partial class AppearanceSettingsPage : UserControl
{
    public AppearanceSettingsPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
