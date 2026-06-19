using FullCompo.Shared.Models.Weather;

namespace FullCompo.Core.Abstractions.Services;

public interface IWeatherService
{
    WeatherInfo? LastWeatherInfo { get; }
    bool IsRefreshing { get; }

    event EventHandler<WeatherInfo?>? WeatherUpdated;

    Task RefreshAsync();
    Task<List<CitySearchResult>> SearchCityAsync(string name);
    string GetWeatherText(int code);
}

public class CitySearchResult
{
    public string CityId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Province { get; set; }
    public string? District { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
}
