using System;
using Avalonia.Controls;
using FullCompo.App.Helpers;
using FullCompo.App.Views;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Shared.Models;

namespace FullCompo.App.Services;

public class PanelService : IPanelService
{
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly IWidgetRegistry _widgetRegistry;
    private DesktopSurfaceWindow? _desktopWindow;

    public IReadOnlyList<Window> WidgetWindows => _desktopWindow != null
        ? new List<Window> { _desktopWindow }.AsReadOnly()
        : new List<Window>().AsReadOnly();

    public bool IsEditMode => _desktopWindow?.IsEditMode ?? false;

    public event EventHandler<bool>? EditModeChanged;

    public PanelService(IServiceProvider services, IConfigService configService, IWidgetRegistry widgetRegistry)
    {
        _services = services;
        _configService = configService;
        _widgetRegistry = widgetRegistry;
    }

    public void CreateOrUpdateWidgets()
    {
        try
        {
            if (_desktopWindow == null)
            {
                _desktopWindow = new DesktopSurfaceWindow(_services);
                _desktopWindow.EditModeChanged += (s, e) => EditModeChanged?.Invoke(s, e);
                _desktopWindow.Show();
            }
            else
            {
                _desktopWindow.LoadWidgets();
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteException("PanelService.CreateOrUpdateWidgets", ex);
        }
    }

    public void EnterEditMode()
    {
        try
        {
            if (_desktopWindow == null)
            {
                CreateOrUpdateWidgets();
            }
            _desktopWindow?.EnterEditMode();
        }
        catch (Exception ex)
        {
            AppLog.WriteException("PanelService.EnterEditMode", ex);
        }
    }

    public void ExitEditMode()
    {
        try
        {
            _desktopWindow?.ExitEditMode();
        }
        catch (Exception ex)
        {
            AppLog.WriteException("PanelService.ExitEditMode", ex);
        }
    }

    public void RemoveWidget(WidgetInstanceConfig config)
    {
        foreach (var panel in _configService.Panels)
        {
            if (panel.Widgets.Remove(config))
                break;
        }
        _configService.Save();
        _desktopWindow?.LoadWidgets();
    }
}
