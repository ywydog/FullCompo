using Avalonia.Controls;
using FullCompo.App.Views;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Shared.Models;

namespace FullCompo.App.Services;

public class PanelService : IPanelService
{
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly List<PanelWindow> _panels = new();

    public IReadOnlyList<Window> Panels => _panels.Cast<Window>().ToList().AsReadOnly();
    public bool IsEditMode { get; private set; }

    public event EventHandler<bool>? EditModeChanged;

    public PanelService(IServiceProvider services, IConfigService configService)
    {
        _services = services;
        _configService = configService;
    }

    public void CreateOrUpdatePanels()
    {
        foreach (var panel in _panels)
        {
            panel.Close();
        }
        _panels.Clear();

        foreach (var config in _configService.Panels)
        {
            var window = new PanelWindow(config, _services);
            _panels.Add(window);
            window.Show();
            window.Activate();
        }
    }

    public void EnterEditMode()
    {
        IsEditMode = true;
        foreach (var panel in _panels)
        {
            panel.SetEditMode(true);
        }
        EditModeChanged?.Invoke(this, true);
    }

    public void ExitEditMode()
    {
        IsEditMode = false;
        foreach (var panel in _panels)
        {
            panel.SetEditMode(false);
        }
        _configService.Save();
        EditModeChanged?.Invoke(this, false);
    }
}
