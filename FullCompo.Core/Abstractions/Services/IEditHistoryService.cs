namespace FullCompo.Core.Abstractions.Services;

public interface IEditHistoryService
{
    bool CanUndo { get; }
    bool CanRedo { get; }

    event EventHandler? StateChanged;

    void RecordState();
    void Undo();
    void Redo();
    void Clear();
}
