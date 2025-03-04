namespace Assistant.WebApp.Infrastructure.OpenAi;

public class OpenAiSettings
{
    public string ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
}

public class OpenAiSettings<TResponse>
{
    public string JsonSchemaFormatName { get; set; } = typeof(TResponse).Name;
    public string SystemMessage { get; set; } = "";
}