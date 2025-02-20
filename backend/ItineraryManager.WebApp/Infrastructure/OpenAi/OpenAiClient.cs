using System.Diagnostics.CodeAnalysis;
using FluentResults;
using ItineraryManager.Domain.Itineraries;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
                    The changes can be Creations, Removals, and Reorderings of activities.
                    Suggested changes include locations, which will need to be searchable in google maps.
                    Activities can not be modified. If an activity should be changes, it has to be removed and added again as separate actions.
                    Include the reasoning for the changes.
                    
                    Model details:
                    - All Id fields are Guids
                    - *_Time are DateTimes, meaning they have the format "yyyy-MM-ddTHH:mm:ss" in the local time of that location. Example: "2025-04-14T15:23:56".
                    - *_Time_TzId are TzId timezone identifiers for that location. Example for paris: "Europe/Paris".
                    - *_Place_Name will be used in google maps searches to find the location.
                    - *_Place_Reference will be updated to use a google maps ID.
                    - *_Place_Description should be a human-readable description of the location of the activity.

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

internal class ChangeRequestResponseModel
{
    public required List<OrderedSuggestion<FlattenedActivityCreation>> Creations { get; init; } = [];
    public required List<OrderedSuggestion<Itinerary.ActivityRemoval>> Removals { get; init; } = [];
    public required List<OrderedSuggestion<Itinerary.ActivityReordering>> Reorderings { get; init; } = [];
    public required string Reasoning { get; init; } = "";

    public IEnumerable<IItineraryChange> OrderByOrder() => 
        Creations.AsGeneric()
        .Concat(Removals.AsGeneric())
        .Concat(Reorderings.AsGeneric())
        .OrderBy(i => i.Order)
        .Select(i => i.Change);
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class FlattenedActivity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required DateTime Start_Time { get; set; }
    public required string Start_Time_TzId { get; set; }
    public required string Start_Place_Reference { get; set; }
    public required string Start_Place_Name { get; set; }
    public required string Start_Place_Description { get; set; }
    public required DateTime End_Time { get; set; }
    public required string End_Time_TzId { get; set; }
    public required string End_Place_Reference { get; set; }
    public required string End_Place_Name { get; set; }
    public required string End_Place_Description { get; set; }
}

internal record FlattenedActivityCreation(FlattenedActivity FlattenedActivity, Guid? PrecedingActivityId = null) : IItineraryChange
{
    public Result Apply(Itinerary itinerary)
    {
        var mappedChange = new Itinerary.ActivityCreation(
            new FlattenedActivityConverter().ConvertFromProviderTyped(FlattenedActivity), PrecedingActivityId);
        return itinerary.Apply(mappedChange);
    }
}

internal class FlattenedActivityConverter() : ValueConverter<Activity, FlattenedActivity>(
    activity => new FlattenedActivity
    {
        Id = activity.Id,
        Name = activity.Name,
        Description = activity.Description,
        Start_Time = activity.Start.Time.ToDateTimeUnspecified(),
        Start_Time_TzId = activity.Start.Time.Zone.Id,
        Start_Place_Reference = activity.Start.Place.Reference,
        Start_Place_Name = activity.Start.Place.Name,
        Start_Place_Description = activity.Start.Place.Description,
        End_Time = activity.End.Time.ToDateTimeUnspecified(),
        End_Time_TzId = activity.End.Time.Zone.Id,
        End_Place_Reference = activity.End.Place.Reference,
        End_Place_Name = activity.End.Place.Name,
        End_Place_Description = activity.End.Place.Description
    }, flattenedActivity => new Activity
    {
        Id = flattenedActivity.Id,
        Name = flattenedActivity.Name,
        Description = flattenedActivity.Description,
        Start = new TimeAndPlace
        {
            Time = LocalDateTime.FromDateTime(flattenedActivity.Start_Time).InZoneStrictly(DateTimeZoneProviders.Tzdb.GetZoneOrNull(flattenedActivity.Start_Time_TzId)!),
            Place = new Place
            {
                Reference = flattenedActivity.Start_Place_Reference,
                Name = flattenedActivity.Start_Place_Name,
                Description = flattenedActivity.Start_Place_Description
            }
        },
        End = new TimeAndPlace
        {
            Time = LocalDateTime.FromDateTime(flattenedActivity.End_Time).InZoneStrictly(DateTimeZoneProviders.Tzdb.GetZoneOrNull(flattenedActivity.End_Time_TzId)!),
            Place = new Place
            {
                Reference = flattenedActivity.End_Place_Reference,
                Name = flattenedActivity.End_Place_Name,
                Description = flattenedActivity.End_Place_Description
            }
        }
    }
);

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