using System.Text.Json;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Shared.Helpers;
using FullCompo.Shared.Models;

namespace FullCompo.Core.Services;

public class EditHistoryService : IEditHistoryService
{
    private readonly IConfigService _configService;
    private readonly List<List<PanelConfig>> _undoStack = new();
    private readonly List<List<PanelConfig>> _redoStack = new();
    private const int MaxHistorySize = 50;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public EditHistoryService(IConfigService configService)
    {
        _configService = configService;
    }

    public void RecordState()
    {
        var snapshot = ClonePanels(_configService.Panels);
        _undoStack.Add(snapshot);
        TrimStack(_undoStack);
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var current = ClonePanels(_configService.Panels);
        var previous = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);

        _redoStack.Add(current);
        TrimStack(_redoStack);

        ApplyState(previous);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var current = ClonePanels(_configService.Panels);
        var next = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);

        _undoStack.Add(current);
        TrimStack(_undoStack);

        ApplyState(next);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyState(List<PanelConfig> panels)
    {
        _configService.Panels.Clear();
        foreach (var panel in ClonePanels(panels))
        {
            _configService.Panels.Add(panel);
        }
        _configService.Save();
    }

    private static List<PanelConfig> ClonePanels(List<PanelConfig> panels)
    {
        var json = JsonSerializer.Serialize(panels, JsonHelper.Options);
        return JsonSerializer.Deserialize<List<PanelConfig>>(json, JsonHelper.Options) ?? new List<PanelConfig>();
    }

    private static void TrimStack(List<List<PanelConfig>> stack)
    {
        while (stack.Count > MaxHistorySize)
        {
            stack.RemoveAt(0);
        }
    }
}
