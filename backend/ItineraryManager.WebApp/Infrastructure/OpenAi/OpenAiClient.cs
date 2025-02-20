using FluentResults;
using ItineraryManager.Domain.Itineraries;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using OpenAI.Chat;

namespace ItineraryManager.WebApp.Infrastructure.OpenAi;

public class OpenAiClient(IOptions<OpenAiSettings> settings)
{
    private ChatCompletionOptions ChatCompletionOptions => new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "itinerary_change_suggestions",
            jsonSchema: BinaryData.FromString(GetResponseJsonSchema()),
            jsonSchemaIsStrict: true)
    };
    
    private static string GetResponseJsonSchema()
    {
        var schemaGenerator = new JSchemaGenerator
        {
            SchemaReferenceHandling = SchemaReferenceHandling.None
        };
        var schema = schemaGenerator.Generate(typeof(ChangeRequestResponseModel));
        DisallowAdditionalPropertiesRecursively(schema);
        return schema.ToString();
    }

    private static void DisallowAdditionalPropertiesRecursively(JSchema? schema)
    {
        if (schema is null) return;
        if (schema.Type is JSchemaType.Object or (JSchemaType.Object | JSchemaType.Null)) schema.AllowAdditionalProperties = false;
        if (schema.Items.Any())
        {
            foreach (var childSchema in schema.Items)
            {
                DisallowAdditionalPropertiesRecursively(childSchema);
            }
        }
        else
        {
            foreach (var childSchema in schema.Properties.Values)
            {
                DisallowAdditionalPropertiesRecursively(childSchema);
            }
        }
    }
    
    public async Task<Result<IEnumerable<IItineraryChange>>> RequestChangeSuggestions(Itinerary itinerary,
        string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var chatClient = new ChatClient(settings.Value.Model, settings.Value.ApiKey);

            List<ChatMessage> messages =
            [
                new SystemChatMessage(
                    """
                    You are an assistant to create travel itinerary change suggestions.
                    The user provides a prompt, and based on that prompt, you will suggest which changes you would make to the provided itinerary to accomodate those prompts.
                    Suggested changes include locations, which will need to be searchable in google maps.

                    The current itinerary is:
                    """),
                new SystemChatMessage(JsonConvert.SerializeObject(itinerary, Formatting.Indented)),
                new UserChatMessage(prompt)
            ];

            var completion = await chatClient.CompleteChatAsync(messages, ChatCompletionOptions, cancellationToken);

            var requestedChanges = JsonConvert.DeserializeObject<ChangeRequestResponseModel>(completion.Value.Content[0].Text);
            if (requestedChanges is null) return Result.Fail("Could not deserialize requested changes");

            return Result.Ok(requestedChanges.OrderByOrder());
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to get suggestion changes from OpenAi").CausedBy(ex));
        }
    }
}

internal class ChangeRequestResponseModel
{
    public List<OrderedSuggestion<Itinerary.ActivityCreation>> Creations { get; init; } = [];
    public List<OrderedSuggestion<Itinerary.ActivityRemoval>> Removals { get; init; } = [];
    public List<OrderedSuggestion<Itinerary.ActivityReordering>> Reorderings { get; init; } = [];

    public IEnumerable<IItineraryChange> OrderByOrder() => 
        Creations.AsGeneric()
        .Concat(Removals.AsGeneric())
        .Concat(Reorderings.AsGeneric())
        .OrderBy(i => i.Order)
        .Select(i => i.Change);
}

internal class OrderedSuggestion<T> where T : IItineraryChange
{
    public required int Order { get; init; }
    public required T Change { get; init; }
}

internal static class OrderedSuggestionExtensions{
    public static IEnumerable<OrderedSuggestion<IItineraryChange>> AsGeneric<TSource>(this IEnumerable<OrderedSuggestion<TSource>> source) where TSource : IItineraryChange =>
        source.Select(orderedSuggestion => new OrderedSuggestion<IItineraryChange>
        {
            Order = orderedSuggestion.Order,
            Change = orderedSuggestion.Change
        });
}