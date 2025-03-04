using Assistant.Domain.Projects;
using FluentResults;
using NodaTime;

namespace Assistant.Domain.Itineraries;

public class Itinerary : Project<object?, Activity>;

public class ItineraryChange(IChange<Itinerary> change) : IChange<Itinerary>
{
    public required IEnumerable<Place> Places { get; init; }
    public Result Apply(Itinerary project) => change.Apply(project);

    public string Description(Itinerary project) => change.Description(project);
}

public class Activity : ProjectItem
{
    public required string Description { get; set; }
    public required TimeAndPlace Start { get; set; }
    public required TimeAndPlace End { get; set; }
}

public class TimeAndPlace
{
    public required ZonedDateTime Time { get; set; }
    public required Place Place { get; set; }
}

public class Place
{
    public required string Reference { get; set; }
    public required string Uri { get; set; }
    public required string SearchQuery { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required float Latitude { get; set; }
    public required float Longitude { get; set; }
}