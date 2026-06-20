using Avalonia.Controls;
using FluentAvalonia.UI.Navigation;
using FullCompo.App.ViewModels;

namespace FullCompo.App.Views;

public sealed class SettingsPageFactory : INavigationPageFactory
{
    private readonly AppSettingsWindowViewModel _viewModel;

    public SettingsPageFactory(AppSettingsWindowViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public object GetPage(Type type)
    {
        if (Activator.CreateInstance(type) is not Control control)
        {
            throw new InvalidOperationException($"Unable to create page of type {type}");
        }

        control.DataContext = _viewModel;
        return control;
    }

    public object GetPageFromObject(object target)
    {
        return target;
    }
}
