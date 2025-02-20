using FluentResults;

namespace ItineraryManager.Domain.Itineraries.Dependencies;

public interface IItineraryChangeProvider
{
    public Task<Result<IEnumerable<IItineraryChange>>> RequestChangeSuggestions(Itinerary itinerary, string prompt,
        CancellationToken cancellationToken);
}