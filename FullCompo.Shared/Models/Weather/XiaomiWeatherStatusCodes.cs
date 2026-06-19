using System.Text.Json.Serialization;

namespace FullCompo.Shared.Models.Weather;

public class XiaomiWeatherStatusCodes
{
    [JsonPropertyName("weatherinfo")]
    public List<XiaomiWeatherStatusCodeItem> WeatherInfo { get; set; } = new();
}
