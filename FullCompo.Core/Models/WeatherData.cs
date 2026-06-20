namespace FullCompo.Core.Models;

public class WeatherData
{
    public string City { get; set; } = "";
    public double Temperature { get; set; }
    public string ConditionText { get; set; } = "";
    public int ConditionCode { get; set; }
    public string Icon { get; set; } = "☁";
}
