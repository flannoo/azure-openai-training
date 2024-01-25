using System.Text.Json;
using Azure.AI.OpenAI;

namespace Chatbot;

public static class WeatherService
{
    public static Weather GetWeather(string location, string unit)
    {
        // return random temperature between 0° - 36° celsius
        var random = new Random();

        return unit switch
        {
            "Fahrenheit" => new Weather() { Temperature = random.Next(32, 97), Unit = unit, Location = location},
            _ => new Weather() { Temperature = random.Next(0, 36), Unit = unit, Location = location }
        };
    }
    
    public static ChatCompletionsToolDefinition GetWeatherFunctionDefinition()
    {
        var unitOptions = new[] { "Celsius", "Fahrenheit" };
        var requiredFields = new[] { "location" };

        return new ChatCompletionsFunctionToolDefinition()
        {
            Name = "GetWeather",
            Description = "Get the current weather temperature for a given location",
            Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city to retrieve the temperature from, e.g. Brussels, BE",
                        },
                        Unit = new
                        {
                            Type = "string",
                            Enum = unitOptions,
                            Description = "The temperature unit which can be Celsius (default) or Fahrenheit",
                        }
                    },
                    Required = requiredFields,
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
        };
    }
}