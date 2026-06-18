using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FullCompo.App.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog(string title, Control settingsView)
    {
        InitializeComponent();

        var titleText = this.FindControl<TextBlock>("TitleText");
        var content = this.FindControl<ContentControl>("SettingsContent");

        if (titleText != null) titleText.Text = title;
        if (content != null) content.Content = settingsView;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
