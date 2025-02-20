using FluentResults;
using ItineraryManager.Domain.Itineraries;
using ItineraryManager.Domain.Itineraries.Dependencies;
using ItineraryManager.WebApp.Infrastructure.GoogleMaps;
using ItineraryManager.WebApp.Infrastructure.OpenAi;

namespace ItineraryManager.WebApp.Infrastructure;

public class ItineraryChangeProvider(OpenAiClient openAiClient, GoogleMapsClient googleMapsClient) : IItineraryChangeProvider
{
    public async Task<Result<IEnumerable<IItineraryChange>>> RequestChangeSuggestions(Itinerary itinerary, string prompt, CancellationToken cancellationToken)
    {
        var changesFromAi = await openAiClient.RequestChangeSuggestions(itinerary, prompt, cancellationToken);
        if (changesFromAi.IsFailed) return changesFromAi;

        return await googleMapsClient.PopulatePlaceInformation(changesFromAi.Value.ToList(), cancellationToken);
    }
}