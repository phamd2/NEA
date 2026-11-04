using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TransportAndWeatherApp
{

    public class AirQualityResponse
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; }

        [JsonPropertyName("forecastSummary")]
        public string ForecastSummary { get; set; }

        [JsonPropertyName("forecastText")]
        public string ForecastText { get; set; }

        [JsonPropertyName("currentForecast")]
        public AirQualityForecast[] CurrentForecast { get; set; }
    }

    public class AirQualityForecast
    {
        [JsonPropertyName("forecastType")]
        public string ForecastType { get; set; }

        [JsonPropertyName("forecastID")]
        public string ForecastID { get; set; }

        [JsonPropertyName("forecastBand")]
        public string ForecastBand { get; set; }

        [JsonPropertyName("forecastSummary")]
        public string ForecastSummary { get; set; }

        [JsonPropertyName("nolineText")]
        public string NolineText { get; set; }
    }

    public class AirQualityService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.tfl.gov.uk/AirQuality/";

        public AirQualityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AirQualityResponse> GetAirQualityAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();

                string jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AirQualityResponse>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TfL Air Quality Error]: {ex.Message}");
                return null;
            }
        }
    }

    public class OpenMeteoResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("current")]
        public OpenMeteoCurrent Current { get; set; }
    }

    public class OpenMeteoCurrent
    {
        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; set; }

        [JsonPropertyName("precipitation")]
        public double Precipitation { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed10m { get; set; }
    }

    public class OpenMeteoWeatherService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

        public OpenMeteoWeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OpenMeteoResponse> GetCurrentWeatherAsync(double latitude = 51.5074, double longitude = -0.1278)
        {
            try
            {
                string requestUrl = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,precipitation,wind_speed_10m";

                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OpenMeteoResponse>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Open Meteo API Error: {ex.Message}");
                return null;
            }
        }
    }


    class Program
    {
        static async Task Main(string[] args)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TransportAndWeatherApp");

                var airQualityService = new AirQualityService(client);
                var weatherService = new OpenMeteoWeatherService(client);


                Console.WriteLine("TFL Air quality");


                AirQualityResponse airQuality = await airQualityService.GetAirQualityAsync();
                if (airQuality != null)
                {
                    Console.WriteLine($"Forecast Summary: {airQuality.ForecastSummary}");
                    if (airQuality.CurrentForecast != null)
                    {
                        foreach (var forecast in airQuality.CurrentForecast)
                        {
                            Console.WriteLine($" -> [{forecast.ForecastType}] Band: {forecast.ForecastBand}");
                        }
                    }
                }

                Console.WriteLine("\n");
                Console.WriteLine("Weather Data London");


                OpenMeteoResponse weather = await weatherService.GetCurrentWeatherAsync(51.5074, -0.1278);

                if (weather != null && weather.Current != null)
                {
                    Console.WriteLine($"Location Coordinates: {weather.Latitude}, {weather.Longitude}");
                    Console.WriteLine($"Observation Time: {weather.Current.Time}");
                    Console.WriteLine($"Temperature: {weather.Current.Temperature2m}°C");
                    Console.WriteLine($"Precipitation: {weather.Current.Precipitation} mm");
                    Console.WriteLine($"Wind Speed: {weather.Current.WindSpeed10m} km/h");
                }
            }
        }
    }
}

