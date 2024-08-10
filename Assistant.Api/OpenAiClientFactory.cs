using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace Assistant.Api;

public class OpenAiClientFactory(IOptions<OpenAiSettings> settings)
{
    private OpenAIClient Client => new(settings.Value.ApiKey);

    public ChatClient GetChatClient() => Client.GetChatClient(settings.Value.ChatModel);

    public AssistantClient GetAssistantClient() => Client.GetAssistantClient();
}
