using System.Diagnostics.CodeAnalysis;
using Assistant.Domain.Itineraries;
using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi.Itineraries;

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
            Places = [change.Item.Start.Place, change.Item.End.Place]
        };
    }
}