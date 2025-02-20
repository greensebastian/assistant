using FluentResults;
using ItineraryManager.Domain.Itineraries;

namespace ItineraryManager.WebApp.Infrastructure.GoogleMaps;

public class GoogleMapsClient
{
    public async Task<Result<IEnumerable<IItineraryChange>>> PopulatePlaceInformation(IEnumerable<IItineraryChange> changes, CancellationToken cancellationToken)
    {
        return Result.Ok(changes);
    }
}