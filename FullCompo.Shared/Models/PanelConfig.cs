using FullCompo.Shared.Enums;

namespace FullCompo.Shared.Models;

public class PanelConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "默认面板";
    public PanelDockMode DockMode { get; set; } = PanelDockMode.TopCenter;
    public double MarginLeft { get; set; }
    public double MarginTop { get; set; } = 8;
    public double MarginRight { get; set; }
    public double MarginBottom { get; set; }
    public double PanelHeight { get; set; } = 80;
    public double Spacing { get; set; } = 8;
    public double CornerRadius { get; set; } = 20;
    public List<WidgetInstanceConfig> Widgets { get; set; } = new();
}
