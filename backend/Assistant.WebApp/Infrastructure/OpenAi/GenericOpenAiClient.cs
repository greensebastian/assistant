using FluentResults;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using OpenAI.Chat;

namespace Assistant.WebApp.Infrastructure.OpenAi;

public class GenericOpenAiClient<TProject, TResponse, TChange>(IOptions<OpenAiSettings> openAiSettings, IOptions<OpenAiSettings<TResponse>> responseSettings) where TResponse : IResponseModel<TChange>
{
    private ChatCompletionOptions ChatCompletionOptions => new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: responseSettings.Value.JsonSchemaFormatName,
            jsonSchema: BinaryData.FromString(GetResponseJsonSchema()),
            jsonSchemaIsStrict: true)
    };

    private static string GetResponseJsonSchema()
    {
        var schemaGenerator = new JSchemaGenerator
        {
            SchemaReferenceHandling = SchemaReferenceHandling.None
        };
        var schema = schemaGenerator.Generate(typeof(TResponse));
        TrimSchemaToOpenAiRestrictions(schema);
        return schema.ToString();
    }

    private static void TrimSchemaToOpenAiRestrictions(JSchema? schema)
    {
        if (schema is null) return;

        // Disallow additional properties on all objects
        if (schema.Type is JSchemaType.Object or (JSchemaType.Object | JSchemaType.Null))
        {
            schema.AllowAdditionalProperties = false;
        }

        // Remove format hint from all string schemas
        if (schema.Type is JSchemaType.String or (JSchemaType.String | JSchemaType.Null))
        {
            schema.Format = null;
        }

        if (schema.Items.Any())
        {
            foreach (var childSchema in schema.Items)
            {
                TrimSchemaToOpenAiRestrictions(childSchema);
            }
        }
        else
        {
            foreach (var childSchema in schema.Properties.Values)
            {
                TrimSchemaToOpenAiRestrictions(childSchema);
            }
        }
    }

    public async Task<Result<IEnumerable<TChange>>> GetChangeSuggestions(TProject project, string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var chatClient = new ChatClient(openAiSettings.Value.Model, openAiSettings.Value.ApiKey);

            List<ChatMessage> messages =
            [
                new SystemChatMessage(TResponse.GetSystemMessage()),
                new SystemChatMessage(JsonConvert.SerializeObject(project)),
                new SystemChatMessage("Suggest changes according the following this user prompt:"),
                new UserChatMessage(prompt)
            ];

            var completion = await chatClient.CompleteChatAsync(messages, ChatCompletionOptions, cancellationToken);

            var requestedChanges = JsonConvert.DeserializeObject<TResponse>(completion.Value.Content[0].Text, new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
            if (requestedChanges is null) return Result.Fail("Could not deserialize requested changes");

            return Result.Ok(requestedChanges.GetChanges());
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to get suggestion changes from OpenAi").CausedBy(ex));
        }
    }
}