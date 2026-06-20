using FullCompo.Shared.Enums;

namespace FullCompo.Shared.Models;

public record WidgetSize
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public WidgetSizeType Type { get; init; }
    public int Columns { get; init; } = 1;
    public int Rows { get; init; } = 1;
    public int Width { get; init; } = 80;
    public int Height { get; init; } = 80;
    public bool IsCircular { get; init; }
}
