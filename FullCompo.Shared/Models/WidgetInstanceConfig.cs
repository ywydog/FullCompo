namespace FullCompo.Shared.Models;

public class WidgetInstanceConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WidgetId { get; set; } = string.Empty;
    public string PanelId { get; set; } = string.Empty;
    public string SizeId { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;
    public WidgetSettings Settings { get; set; } = new();
}
