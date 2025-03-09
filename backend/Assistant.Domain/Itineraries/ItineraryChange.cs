using Assistant.Domain.Projects;
using FluentResults;

namespace Assistant.Domain.Itineraries;

public class ItineraryChange(IChange<Itinerary> change) : IChange<Itinerary>
{
    public required IEnumerable<Place> Places { get; init; }
    public Result ApplyTo(Itinerary project) => change.ApplyTo(project);

    public string Description(Itinerary project) => change.Description(project);
}