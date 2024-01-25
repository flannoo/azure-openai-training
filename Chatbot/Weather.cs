namespace Chatbot;

public class Weather
{
    public string Location { get; init; } = string.Empty;
    public int Temperature { get; init; }
    public string Unit { get; init; } = "Celsius";
}