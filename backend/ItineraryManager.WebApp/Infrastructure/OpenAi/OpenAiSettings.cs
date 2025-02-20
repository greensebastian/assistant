namespace ItineraryManager.WebApp.Infrastructure.OpenAi;

public class OpenAiSettings
{
    public string ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
}