namespace Assistant.Api;

public class OpenAiSettings
{
    public const string SectionName = "OpenAi";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    
    public string AssistantModel { get; set; } = "gpt-4o-mini";
}
