using FluentResults;
using Google.Api.Gax.Grpc;
using Google.Maps.Places.V1;
using ItineraryManager.Domain.Itineraries;
using Microsoft.Extensions.Caching.Hybrid;
using GoogleMapsPlace = Google.Maps.Places.V1.Place;
using Place = ItineraryManager.Domain.Itineraries.Place;

namespace ItineraryManager.WebApp.Infrastructure.GoogleMaps;

public class GoogleMapsClient(PlacesClient client, HybridCache placeCache)
{
    public async Task<Result<IEnumerable<IItineraryChange>>> PopulatePlaceInformation(IReadOnlyList<IItineraryChange> changes, CancellationToken cancellationToken)
    {
        foreach (var place in changes.SelectMany(change => change.Places()))
        {
            var result = await Result.Try(() => placeCache.GetOrCreateAsync<GoogleMapsPlace>(place.SearchQuery, async token =>
            {
                var placeSearchResponse = await client.SearchTextAsync(new SearchTextRequest
                {
                    LanguageCode = "en",
                    TextQuery = place.SearchQuery,
                    MaxResultCount = 1
                }, CallSettings.FromFieldMask("places.id,places.displayName,places.googleMapsUri").MergedWith(CallSettings.FromCancellationToken(token)));
            
                return placeSearchResponse.Places.Single();
            }, cancellationToken: cancellationToken));

            if (result.IsFailed) return Result.Fail(result.Errors);
            PopulatePlace(place, result.Value);
        }
        
        return Result.Ok(changes.AsEnumerable());
    }

    private void PopulatePlace(Place placeToPopulate, GoogleMapsPlace mapsPlace)
    {
        placeToPopulate.Reference = mapsPlace.Id;
        placeToPopulate.Name = mapsPlace.DisplayName.Text;
        placeToPopulate.Uri = mapsPlace.GoogleMapsUri;
    }
}