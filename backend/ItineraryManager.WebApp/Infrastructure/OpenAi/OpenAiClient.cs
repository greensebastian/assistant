using System.Diagnostics.CodeAnalysis;
using FluentResults;
using ItineraryManager.Domain.Itineraries;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NodaTime;
using NodaTime.Serialization.JsonNet;
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
                    The changes can be Creation, Removal, Reordering, and Rescheduling of activities.
                    When changing times, make sure other activities also have their start/end times changed accordingly.
                    Suggested changes include locations, which will need to be searchable in google maps.
                    If an activity should be changed beyond rescheduling, it has to be removed and added again as two separate actions.
                    Include the reasoning for the changes in a way that can be presented to the user.
                    
                    Model details:
                    - All Id fields are strings of maximum 5 characters and MUST BE UNIQUE. When replacing an activity, the new activity should have a new Id
                    - *_Time are DateTimes, meaning they have the format "yyyy-MM-ddTHH:mm:ss" in the local time of that location. Example: "2025-04-14T15:23:56".
                    - *_Time_TzId MUST BE valid IANA timezone identifiers (TzId) for that location. The TzId is often tied to the capital of the country. Examples: "Europe/Paris", "Etc/UTC", "Asia/Singapore".
                    - *_Place_SearchQuery will be used to find the place in google maps, should contain a specific name suffixed by district, city, region, or country.
                    - *_Place_Description is a short human-readable description of the place.

                    The current itinerary is:
                    """),
                new SystemChatMessage(JsonConvert.SerializeObject(itinerary, Formatting.Indented)),
                new UserChatMessage($"User prompt: {prompt}")
            ];

            var completion = await chatClient.CompleteChatAsync(messages, ChatCompletionOptions, cancellationToken);

            var requestedChanges = JsonConvert.DeserializeObject<ChangeRequestResponseModel>(completion.Value.Content[0].Text, new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
            if (requestedChanges is null) return Result.Fail("Could not deserialize requested changes");

            return Result.Ok(requestedChanges.OrderByOrder());
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to get suggestion changes from OpenAi").CausedBy(ex));
        }
    }
}

internal interface IItineraryChangeAdapter
{
    public IItineraryChange GetItineraryChange();
}

internal class ChangeRequestResponseModel
{
    public required List<OrderedChangeAdapter<FlattenedActivityCreation>> Creations { get; init; } = [];
    public required List<OrderedChange<Itinerary.ActivityRemoval>> Removals { get; init; } = [];
    public required List<OrderedChange<Itinerary.ActivityReordering>> Reorderings { get; init; } = [];
    public required List<OrderedChangeAdapter<FlattenedActivityRescheduling>> Reschedulings { get; init; } = [];
    public required string Reasoning { get; init; } = "";

    public IEnumerable<IItineraryChange> OrderByOrder() =>
        Creations.AsIItineraryChange()
        .Concat(Removals.AsIItineraryChange())
        .Concat(Reorderings.AsIItineraryChange())
        .Concat(Reschedulings.AsIItineraryChange())
        .OrderBy(i => i.Order)
        .Select(i => i.Change)
        .ToArray();
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class FlattenedActivityCreation : IItineraryChangeAdapter
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required DateTime Start_Time { get; set; }
    public required string Start_Time_TzId { get; set; }
    public required string Start_Place_SearchQuery { get; set; }
    public required string Start_Place_Description { get; set; }
    public required DateTime End_Time { get; set; }
    public required string End_Time_TzId { get; set; }
    public required string End_Place_SearchQuery { get; set; }
    public required string End_Place_Description { get; set; }
    public string? PrecedingActivityId { get; set; } = null;

    public IItineraryChange GetItineraryChange()
    {
        return new Itinerary.ActivityCreation(new Activity
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Start = new TimeAndPlace
            {
                Time = Start_Time.ToZonedDateTime(Start_Time_TzId),
                Place = new Place
                {
                    Reference = "",
                    SearchQuery = Start_Place_SearchQuery,
                    Name = "",
                    Description = Start_Place_Description,
                    Uri = ""
                }
            },
            End = new TimeAndPlace
            {
                Time = End_Time.ToZonedDateTime(End_Time_TzId),
                Place = new Place
                {
                    Reference = "",
                    SearchQuery = End_Place_SearchQuery,
                    Name = "",
                    Description = End_Place_Description,
                    Uri = ""
                }
            }
        }, PrecedingActivityId);
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class FlattenedActivityRescheduling : IItineraryChangeAdapter
{
    public required string ActivityId { get; set; }
    public required DateTime Start_Time { get; set; }
    public required string Start_Time_TzId { get; set; }
    public required DateTime End_Time { get; set; }
    public required string End_Time_TzId { get; set; }

    public IItineraryChange GetItineraryChange() =>
        new Itinerary.ActivityRescheduling(ActivityId, Start_Time.ToZonedDateTime(Start_Time_TzId),
            End_Time.ToZonedDateTime(End_Time_TzId));
}

internal class OrderedChange<T> where T : IItineraryChange
{
    public required int Order { get; init; }
    public required T Change { get; init; }
}

internal class OrderedChangeAdapter<T> where T : IItineraryChangeAdapter
{
    public required int Order { get; init; }
    public required T Change { get; init; }
}

internal static class OrderedChangeExtensions{
    public static IEnumerable<OrderedChange<IItineraryChange>> AsIItineraryChange<TSource>(this IEnumerable<OrderedChange<TSource>>? source) where TSource : IItineraryChange =>
        source?.Select(orderedSuggestion => new OrderedChange<IItineraryChange>
        {
            Order = orderedSuggestion.Order,
            Change = orderedSuggestion.Change
        }) ?? [];
    
    public static IEnumerable<OrderedChange<IItineraryChange>> AsIItineraryChange<TSource>(this IEnumerable<OrderedChangeAdapter<TSource>>? source) where TSource : IItineraryChangeAdapter =>
        source?.Select(orderedSuggestion => new OrderedChange<IItineraryChange>
        {
            Order = orderedSuggestion.Order,
            Change = orderedSuggestion.Change.GetItineraryChange()
        }) ?? [];
}