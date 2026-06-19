using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;
using Avalonia.Threading;
using FullCompo.App.Helpers;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Shared.Models.Weather;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FullCompo.App.Services;

public class WeatherService : IHostedService, IWeatherService
{
    private readonly IConfigService _configService;
    private readonly ILogger<WeatherService> _logger;
    private readonly HttpClient _httpClient = new();
    private readonly DispatcherTimer _updateTimer;

    private List<XiaomiWeatherStatusCodeItem> _statusCodes = new();

    public WeatherInfo? LastWeatherInfo { get; private set; }
    public bool IsRefreshing { get; private set; }

    public event EventHandler<WeatherInfo?>? WeatherUpdated;

    public WeatherService(IConfigService configService, ILogger<WeatherService> logger)
    {
        _configService = configService;
        _logger = logger;
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _updateTimer.Tick += async (_, _) => await RefreshAsync();

        LoadStatusCodes();
    }

    private void LoadStatusCodes()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://FullCompo.App/Assets/XiaomiWeather/xiaomi_weather_status.json"));
            var codes = JsonSerializer.Deserialize<XiaomiWeatherStatusCodes>(stream);
            if (codes?.WeatherInfo != null)
            {
                _statusCodes = codes.WeatherInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load weather status codes");
            AppLog.WriteException("WeatherService.LoadStatusCodes", ex);
        }
    }

    public string GetWeatherText(int code)
    {
        var item = _statusCodes.FirstOrDefault(x => x.Code == code);
        if (item != null) return item.Weather;

        // Fallback to expanded codes like ClassIsland
        var expanded = ExpandWeatherCode(code.ToString());
        foreach (var c in expanded)
        {
            if (int.TryParse(c, out var expandedCode))
            {
                item = _statusCodes.FirstOrDefault(x => x.Code == expandedCode);
                if (item != null) return item.Weather;
            }
        }
        return "未知";
    }

    private static HashSet<string> ExpandWeatherCode(string weatherCode)
    {
        return weatherCode switch
        {
            "21" => new HashSet<string> { "7", "8", "21" },
            "22" => new HashSet<string> { "8", "9", "22" },
            "23" => new HashSet<string> { "9", "10", "23" },
            "24" => new HashSet<string> { "10", "11", "24" },
            "25" => new HashSet<string> { "11", "12", "25" },
            "26" => new HashSet<string> { "14", "15", "26" },
            "27" => new HashSet<string> { "15", "16", "27" },
            "28" => new HashSet<string> { "16", "17", "28" },
            "301" => new HashSet<string> { "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "19", "21", "22", "23", "24", "25", "301" },
            "302" => new HashSet<string> { "6", "13", "14", "15", "16", "17", "26", "27", "28", "34", "302" },
            _ => new HashSet<string> { weatherCode }
        };
    }

    public async Task RefreshAsync()
    {
        if (IsRefreshing) return;
        if (!_configService.AppSettings.WeatherEnabled) return;

        IsRefreshing = true;
        try
        {
            var schema = _configService.AppSettings.NoTLSWeatherRequests ? "http" : "https";
            var cityId = _configService.AppSettings.WeatherCityId;

            // 1. Get city geo info
            var cityUri = string.IsNullOrWhiteSpace(cityId)
                ? $"{schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/geo?longitude={_configService.AppSettings.WeatherLongitude}&latitude={_configService.AppSettings.WeatherLatitude}&locale=zh_cn"
                : $"{schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/info?locationKey={cityId}&locale=zh_cn";

            var cityJson = await _httpClient.GetStringAsync(cityUri);
            var cityInfo = JsonSerializer.Deserialize<JsonElement>(cityJson);

            var latitude = cityInfo.GetProperty("data").GetProperty("city").GetProperty("latitude").GetDouble();
            var longitude = cityInfo.GetProperty("data").GetProperty("city").GetProperty("longitude").GetDouble();
            var locationKey = cityInfo.GetProperty("data").GetProperty("city").GetProperty("locationKey").GetString() ?? cityId;
            var cityName = cityInfo.GetProperty("data").GetProperty("city").GetProperty("name").GetString() ?? _configService.AppSettings.WeatherCityName;

            // 2. Get weather data
            var weatherUri = $"{schema}://weatherapi.market.xiaomi.com/wtr-v3/weather/all?latitude={latitude}&longitude={longitude}&locationKey={Uri.EscapeDataString(locationKey)}&days=15&appKey=weather20151024&sign=zUFJoAR2ZVrDy1vF3D07&isGlobal=false&locale=zh_cn";
            var weatherJson = await _httpClient.GetStringAsync(weatherUri);
            var weatherData = JsonSerializer.Deserialize<JsonElement>(weatherJson);

            LastWeatherInfo = ParseWeatherInfo(weatherData);
            LastWeatherInfo.UpdateTime = DateTime.Now;

            // Update city name in settings
            _configService.AppSettings.WeatherCityName = cityName;
            _configService.AppSettings.WeatherCityId = locationKey;
            _configService.AppSettings.WeatherLatitude = latitude;
            _configService.AppSettings.WeatherLongitude = longitude;

            WeatherUpdated?.Invoke(this, LastWeatherInfo);
            _logger.LogInformation("Weather refreshed for {City}: {Weather} {Temp}",
                cityName,
                LastWeatherInfo.Current.WeatherText,
                LastWeatherInfo.Current.Temperature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh weather");
            AppLog.WriteException("WeatherService.RefreshAsync", ex);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private WeatherInfo ParseWeatherInfo(JsonElement data)
    {
        var info = new WeatherInfo();

        try
        {
            if (data.TryGetProperty("current", out var current))
            {
                info.Current.WeatherCode = current.GetProperty("weather").GetInt32();
                info.Current.WeatherText = GetWeatherText(info.Current.WeatherCode);
                info.Current.Temperature.Value = current.GetProperty("temperature").GetProperty("value").GetDouble();
                info.Current.Temperature.Unit = current.GetProperty("temperature").GetProperty("unit").GetString() ?? "°C";
            }

            if (data.TryGetProperty("forecastDaily", out var forecastDaily) &&
                forecastDaily.TryGetProperty("days", out var days))
            {
                foreach (var day in days.EnumerateArray())
                {
                    info.ForecastDaily.Add(new DailyForecast
                    {
                        Date = DateTimeOffset.FromUnixTimeSeconds(day.GetProperty("time").GetInt64()).DateTime,
                        WeatherCodeDay = day.GetProperty("weatherDay").GetInt32(),
                        WeatherCodeNight = day.GetProperty("weatherNight").GetInt32(),
                        High = new TemperatureValue
                        {
                            Value = day.GetProperty("temperature").GetProperty("high").GetDouble(),
                            Unit = "°C"
                        },
                        Low = new TemperatureValue
                        {
                            Value = day.GetProperty("temperature").GetProperty("low").GetDouble(),
                            Unit = "°C"
                        }
                    });
                }
            }

            if (data.TryGetProperty("aqi", out var aqi))
            {
                info.Aqi = new AirQuality
                {
                    Aqi = aqi.GetProperty("value").GetInt32(),
                    Level = aqi.GetProperty("level").GetString() ?? "",
                    Description = aqi.GetProperty("desc").GetString() ?? ""
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse weather data");
        }

        return info;
    }

    public async Task<List<CitySearchResult>> SearchCityAsync(string name)
    {
        var results = new List<CitySearchResult>();
        if (string.IsNullOrWhiteSpace(name)) return results;

        try
        {
            var schema = _configService.AppSettings.NoTLSWeatherRequests ? "http" : "https";
            var uri = $"{schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/search?name={Uri.EscapeDataString(name)}&locale=zh_cn";
            var json = await _httpClient.GetStringAsync(uri);
            var doc = JsonSerializer.Deserialize<JsonElement>(json);

            if (doc.TryGetProperty("data", out var data))
            {
                foreach (var city in data.EnumerateArray())
                {
                    results.Add(new CitySearchResult
                    {
                        CityId = city.GetProperty("locationKey").GetString() ?? "",
                        Name = city.GetProperty("name").GetString() ?? "",
                        Province = city.TryGetProperty("province", out var p) ? p.GetString() : null,
                        District = city.TryGetProperty("district", out var d) ? d.GetString() : null,
                        Longitude = city.GetProperty("longitude").GetDouble(),
                        Latitude = city.GetProperty("latitude").GetDouble()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search city");
            AppLog.WriteException("WeatherService.SearchCityAsync", ex);
        }

        return results;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _updateTimer.Start();
        _ = RefreshAsync();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _updateTimer.Stop();
        return Task.CompletedTask;
    }
}
