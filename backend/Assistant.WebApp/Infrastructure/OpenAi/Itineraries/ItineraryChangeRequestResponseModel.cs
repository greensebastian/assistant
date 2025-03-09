using Assistant.Domain.Itineraries;
using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi.Itineraries;

internal class ItineraryChangeRequestResponseModel : IResponseModel<ItineraryChange>
{
    public required List<OrderedChangeAdapter<Itinerary, ItineraryChange, ActivityCreationResponse>> Creations { get; init; } = [];
    public required List<OrderedChange<Itinerary, ItemRemoval<Itinerary, string?, Activity>>> Removals { get; init; } = [];
    public required List<OrderedChange<Itinerary, ItemReordering<Itinerary, string?, Activity>>> Reorderings { get; init; } = [];
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
}