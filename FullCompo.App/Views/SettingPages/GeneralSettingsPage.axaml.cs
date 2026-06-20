using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FullCompo.App.Views.SettingPages;

public partial class GeneralSettingsPage : UserControl
{
    public GeneralSettingsPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
