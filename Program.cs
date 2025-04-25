using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class WeatherData
{
    public string City { get; set; }
    public DateTime Date { get; set; }
    public double Temperature { get; set; }
    public string Description { get; set; }
}

class WeatherRequest
{
    public string City { get; set; }
    public DateTime RequestDate { get; set; }
    public WeatherData Weather { get; set; }
}

class Program
{
    static List<WeatherRequest> history = new List<WeatherRequest>();
    static string filePath = "weather_history.json";
    static HttpClient httpClient = new HttpClient();
    static string apiKey = ""; 

    static void Main(string[] args)
    {
        // Загрузка истории запросов из файла
        LoadHistory();

        while (true)
        {
            Console.WriteLine("\n1. Получить текущую погоду для города");
            Console.WriteLine("2. Получить прогноз на 5 дней");
            Console.WriteLine("3. Просмотреть историю запросов");
            Console.WriteLine("4. Сохранить и выйти");
            Console.Write("Выберите действие: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Введите название города: ");
                    string city = Console.ReadLine();
                    Task.Run(async () => await GetWeather(city)).Wait(); // Синхронный вызов для консоли
                    break;
                case "2":
                    Console.Write("Введите название города: ");
                    city = Console.ReadLine();
                    Task.Run(async () => await GetForecast(city)).Wait();
                    break;
                case "3":
                    DisplayHistory();
                    break;
                case "4":
                    SaveHistory();
                    return;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }
    }

    static async Task GetWeather(string city)
    {
        try
        {
            string url = $"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";
            var response = await httpClient.GetFromJsonAsync<Dictionary<string, object>>(url);

            if (response == null)
            {
                Console.WriteLine("Не удалось получить данные о погоде.");
                return;
            }

            var weather = new WeatherData
            {
                City = city,
                Date = DateTime.Now,
                Temperature = ((JsonElement)response["main"]).GetProperty("temp").GetDouble(),
                Description = ((JsonElement)response["weather"])[0].GetProperty("description").GetString()
            };

            var request = new WeatherRequest
            {
                City = city,
                RequestDate = DateTime.Now,
                Weather = weather
            };
            history.Add(request);

            Console.WriteLine($"\nПогода в {weather.City} на {weather.Date}:");
            Console.WriteLine($"Температура: {weather.Temperature}°C, {weather.Description}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при запросе погоды: {ex.Message}");
        }
    }

    static async Task GetForecast(string city)
    {
        try
        {
            string url = $"http://api.openweathermap.org/data/2.5/forecast?q={city}&appid={apiKey}&units=metric";
            var response = await httpClient.GetFromJsonAsync<Dictionary<string, object>>(url);

            if (response == null || !response.ContainsKey("list"))
            {
                Console.WriteLine("Не удалось получить прогноз.");
                return;
            }

            var forecastList = (response["list"] as System.Text.Json.JsonElement?)?.EnumerateArray();
            Console.WriteLine($"\nПрогноз на 5 дней для {city}:");
            foreach (var item in forecastList)
            {
                var date = DateTime.Parse(item.GetProperty("dt_txt").GetString());
                var temp = item.GetProperty("main").GetProperty("temp").GetDouble();
                var desc = item.GetProperty("weather")[0].GetProperty("description").GetString();
                Console.WriteLine($"{date}: {temp}°C, {desc}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при запросе прогноза: {ex.Message}");
        }
    }

    static void DisplayHistory()
    {
        Console.WriteLine("\nИстория запросов:");
        foreach (var request in history)
        {
            Console.WriteLine($"{request.RequestDate}: {request.City}, {request.Weather.Temperature}°C, {request.Weather.Description}");
        }
    }

    static void SaveHistory()
    {
        string json = JsonSerializer.Serialize(history);
        File.WriteAllText(filePath, json);
    }

    static void LoadHistory()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            history = JsonSerializer.Deserialize<List<WeatherRequest>>(json) ?? new List<WeatherRequest>();
        }
    }
}