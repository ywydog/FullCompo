using System.Text.Json;
using System.Text.Json.Serialization;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Helpers;
using FullCompo.Core.Models;
using Microsoft.Extensions.Logging;

namespace FullCompo.Core.Services;

public sealed class WeatherService : IWeatherService, IDisposable
{
    private readonly IConfigService _configService;
    private readonly ILogger<WeatherService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private const string BaseUrl = "https://weatherapi.market.xiaomi.com/wtr-v3/weather/all";
    private const string AppKey = "zUFJoAR2ZVrDy1vF3D07";

    public WeatherData CurrentWeather { get; private set; } = new();

    public event EventHandler? WeatherUpdated;

    public WeatherService(IConfigService configService, ILogger<WeatherService> logger)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        UpdateInterval();
        _ = RefreshAsync();
    }

    public void UpdateInterval()
    {
        var minutes = Math.Clamp(_configService.AppSettings.WeatherRefreshIntervalMinutes, 1, 3);
        _timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(minutes));
    }

    public async Task RefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            var cityName = _configService.AppSettings.WeatherCity;
            var cityCode = WeatherCityCodes.GetCode(cityName);
            var url = $"{BaseUrl}?locationKey=weathercn:{cityCode}&appKey={AppKey}&sign={AppKey}&isGlobal=false&locale=zh_cn&days=1&latitude=0&longitude=0";

            _logger.LogDebug("Fetching weather from {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<XiaomiWeatherResponse>(json, JsonOptions);

            if (data?.Current == null)
            {
                _logger.LogWarning("Weather API returned empty current data");
                return;
            }

            var codeText = data.Current.Weather ?? "99";
            var code = int.TryParse(codeText, out var parsedCode) ? parsedCode : 99;

            CurrentWeather = new WeatherData
            {
                City = cityName,
                Temperature = ParseTemperature(data.Current.Temperature?.Value),
                ConditionText = WeatherDescriptions.GetValueOrDefault(code, "未知"),
                ConditionCode = code,
                Icon = WeatherIcons.GetValueOrDefault(code, "☁")
            };

            _logger.LogInformation("Weather updated: {City} {Icon} {Temperature}°C {Condition}",
                CurrentWeather.City, CurrentWeather.Icon, CurrentWeather.Temperature, CurrentWeather.ConditionText);

            WeatherUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh weather");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static double ParseTemperature(string? value)
    {
        if (double.TryParse(value, out var temp))
        {
            return temp;
        }
        return 0;
    }

    private async void OnTimerElapsed(object? state)
    {
        await RefreshAsync();
    }

    public void Dispose()
    {
        _timer.Dispose();
        _httpClient.Dispose();
        _refreshLock.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly Dictionary<int, string> WeatherDescriptions = new()
    {
        [0] = "晴",
        [1] = "多云",
        [2] = "阴",
        [3] = "阵雨",
        [4] = "雷阵雨",
        [5] = "阵雨伴有冰雹",
        [6] = "雨夹雪",
        [7] = "小雨",
        [8] = "中雨",
        [9] = "大雨",
        [10] = "暴雨",
        [11] = "大暴雨",
        [12] = "特大暴雨",
        [13] = "阵雪",
        [14] = "小雪",
        [15] = "中雪",
        [16] = "大雪",
        [17] = "暴雪",
        [18] = "雾",
        [19] = "冻雨",
        [20] = "沙尘暴",
        [21] = "小到中雨",
        [22] = "中到大雨",
        [23] = "大到暴雨",
        [24] = "暴雨到大暴雨",
        [25] = "大暴雨到特大暴雨",
        [26] = "小到中雪",
        [27] = "中到大雪",
        [28] = "大到暴雪",
        [29] = "浮尘",
        [30] = "扬沙",
        [31] = "强沙尘暴",
        [53] = "霾",
        [99] = "未知"
    };

    private static readonly Dictionary<int, string> WeatherIcons = new()
    {
        [0] = "☀️",
        [1] = "🌤️",
        [2] = "☁️",
        [3] = "🌦️",
        [4] = "⛈️",
        [5] = "⛈️",
        [6] = "🌨️",
        [7] = "🌧️",
        [8] = "🌧️",
        [9] = "🌧️",
        [10] = "🌧️",
        [11] = "⛈️",
        [12] = "⛈️",
        [13] = "🌨️",
        [14] = "❄️",
        [15] = "❄️",
        [16] = "❄️",
        [17] = "❄️",
        [18] = "🌫️",
        [19] = "🌧️",
        [20] = "🌫️",
        [21] = "🌧️",
        [22] = "🌧️",
        [23] = "🌧️",
        [24] = "⛈️",
        [25] = "⛈️",
        [26] = "🌨️",
        [27] = "🌨️",
        [28] = "🌨️",
        [29] = "🌫️",
        [30] = "🌫️",
        [31] = "🌫️",
        [53] = "🌫️",
        [99] = "☁️"
    };

    private class XiaomiWeatherResponse
    {
        [JsonPropertyName("current")] public CurrentWeatherData? Current { get; set; }
    }

    private class CurrentWeatherData
    {
        [JsonPropertyName("weather")] public string? Weather { get; set; }
        [JsonPropertyName("temperature")] public ValueUnitData? Temperature { get; set; }
    }

    private class ValueUnitData
    {
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
    }
}
