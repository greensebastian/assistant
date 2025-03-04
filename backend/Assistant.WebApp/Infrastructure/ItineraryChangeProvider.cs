using Assistant.WebApp.Infrastructure.GoogleMaps;
using FluentResults;
using Assistant.Domain.Itineraries;
using Assistant.Domain.Projects;
using Itinerary = Assistant.Domain.Itineraries.Itinerary;

namespace Assistant.WebApp.Infrastructure;

public class ItineraryChangeProvider(OpenAiClient openAiClient, GoogleMapsClient googleMapsClient) : IChangeProvider<Itinerary, ItineraryChange>
{
    public async Task<Result<IEnumerable<ItineraryChange>>> GetChangeSuggestions(Itinerary project, string prompt, CancellationToken cancellationToken)
    {
        var changesFromAi = await openAiClient.RequestChangeSuggestions(project, prompt, cancellationToken);
        if (changesFromAi.IsFailed) return changesFromAi;

        return await googleMapsClient.PopulatePlaceInformation(changesFromAi.Value.ToList(), cancellationToken);
    }
}