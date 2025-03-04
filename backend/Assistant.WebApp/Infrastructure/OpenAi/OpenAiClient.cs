using System.Diagnostics.CodeAnalysis;
using FluentResults;
using Assistant.Domain.Itineraries;
using Assistant.Domain.Projects;
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
                new SystemChatMessage(responseSettings.Value.SystemMessage),
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

public interface IResponseModel<out TChange>
{
    public IEnumerable<TChange> GetChanges();

    public string GetReasoning();

    public static abstract string GetSystemMessage();
}

internal interface IChangeAdapter<TProject, out TChange> where TChange : IChange<TProject>
{
    public TChange GetChange();
}

internal class ItineraryChangeRequestResponseModel : IResponseModel<ItineraryChange>
{
    public required List<OrderedChangeAdapter<Itinerary, ItineraryChange, ActivityCreationResponse>> Creations { get; init; } = [];
    public required List<OrderedChange<Itinerary, ItemRemoval<Itinerary, object?, Activity>>> Removals { get; init; } = [];
    public required List<OrderedChange<Itinerary, ItemReordering<Itinerary, object?, Activity>>> Reorderings { get; init; } = [];
    public required string Reasoning { get; init; } = "";

    public IEnumerable<ItineraryChange> GetChanges()
    {
        var creations = Creations.Select(c => new OrderedChange<Itinerary, ItineraryChange>
        {
            Order = c.Order,
            Change = c.Change.GetChange()
        });
        
        List<OrderedChange<Itinerary, ItineraryChange>> results =
        [
            ..creations,
            ..Removals.Select(c => new OrderedChange<Itinerary, ItineraryChange>()
            {
                Order = c.Order,
                Change = new ItineraryChange(c.Change) { Places = [] }
            }),
            ..Reorderings.Select(c => new OrderedChange<Itinerary, ItineraryChange>()
            {
                Order = c.Order,
                Change = new ItineraryChange(c.Change) { Places = [] }
            })
        ];

        return results.OrderBy(c => c.Order).Select(c => c.Change);
    }

    public string GetReasoning() => Reasoning;

    public static string GetSystemMessage() => """
                                               You are an assistant to create travel itinerary change suggestions.
                                               The user provides a prompt, and based on that prompt, you will suggest which changes you would make to the provided itinerary to accomodate those prompts.
                                               The changes can be Creation, Removal, Reordering, and Rescheduling of activities.
                                               When changing times, make sure other activities also have their start/end times changed accordingly.
                                               Suggested changes include locations, which will need to be searchable in google maps.
                                               If an activity should be changed beyond rescheduling, it has to be removed and added again as two separate actions.
                                               Include the reasoning for the changes in a way that can be presented to the user.

                                               Model details:
                                               - All Id fields are unique numbers. When replacing an activity, the new activity should have a new Id
                                               - *_Time are DateTimes, meaning they have the format "yyyy-MM-ddTHH:mm:ss" in the local time of that location. Example: "2025-04-14T15:23:56".
                                               - *_Time_TzId MUST BE valid IANA timezone identifiers (TzId) for that location. The TzId is often tied to the capital of the country. Examples: "Europe/Paris", "Etc/UTC", "Asia/Singapore".
                                               - *_Place_SearchQuery will be used to find the place in google maps, should contain a specific name suffixed by district, city, region, or country.
                                               - *_Place_Description is a short human-readable description of the place.
                                               """;
    private OrderedChange<Itinerary, ItineraryChange> GetChange(OrderedChange<Itinerary, ItineraryChange> change)
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class ActivityCreationResponse : IChangeAdapter<Itinerary, ItineraryChange>
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

    public ItineraryChange GetChange()
    {
        var change = new ItemAddition<Itinerary, object?, Activity>(new Activity
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
                    Uri = "",
                    Latitude = 0f,
                    Longitude = 0f
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
                    Uri = "",
                    Latitude = 0f,
                    Longitude = 0f
                }
            }
        }, PrecedingActivityId);
        
        return new ItineraryChange(change)
        {
            Places = [ change.Item.Start.Place, change.Item.End.Place ]
        };
    }
}

internal class OrderedChange<TProject, TChange> where TChange : IChange<TProject>
{
    public required int Order { get; init; }
    public required TChange Change { get; init; }
}

internal class OrderedChangeAdapter<TProject, TChange, TChangeAdapter> where TChangeAdapter : IChangeAdapter<TProject, TChange> where TChange : IChange<TProject>
{
    public required int Order { get; init; }
    public required TChangeAdapter Change { get; init; }
}