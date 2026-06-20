using FullCompo.Core.Models;

namespace FullCompo.Core.Abstractions.Services;

public interface IWeatherService
{
    WeatherData CurrentWeather { get; }
    event EventHandler? WeatherUpdated;
    Task RefreshAsync();
    void UpdateInterval();
}
