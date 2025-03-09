using FluentResults;
using Google.Api.Gax.Grpc;
using Google.Maps.Places.V1;
using Assistant.Domain.Itineraries;
using Assistant.Domain.Projects;
using Microsoft.Extensions.Caching.Hybrid;
using GoogleMapsPlace = Google.Maps.Places.V1.Place;
using Place = Assistant.Domain.Itineraries.Place;

namespace Assistant.WebApp.Infrastructure.GoogleMaps;

public class GoogleMapsClient(ILogger<GoogleMapsClient> logger, PlacesClient client, HybridCache placeCache) : IChangeProcessor<Itinerary, ItineraryChange>
{
    private void PopulatePlace(Place placeToPopulate, GoogleMapsPlace mapsPlace)
    {
        placeToPopulate.Reference = mapsPlace.Id;
        placeToPopulate.Name = mapsPlace.DisplayName.Text;
        placeToPopulate.Uri = mapsPlace.GoogleMapsUri;
        placeToPopulate.Latitude = (float)mapsPlace.Location.Latitude;
        placeToPopulate.Longitude = (float)mapsPlace.Location.Longitude;
    }

    public async Task<Result<IEnumerable<ItineraryChange>>> Process(Itinerary project, IReadOnlyList<ItineraryChange> changes, CancellationToken cancellationToken)
    {
        foreach (var place in changes.SelectMany(change => change.Places))
        {
            logger.LogInformation("Looking for place {@Place}", place);
            var result = await Result.Try(() => placeCache.GetOrCreateAsync<GoogleMapsPlace>(place.SearchQuery, async token =>
            {
                var placeSearchResponse = await client.SearchTextAsync(new SearchTextRequest
                {
                    LanguageCode = "en",
                    TextQuery = place.SearchQuery,
                    MaxResultCount = 1
                }, CallSettings.FromFieldMask("places.id,places.displayName,places.googleMapsUri,places.location").MergedWith(CallSettings.FromCancellationToken(token)));

                var result = placeSearchResponse.Places.SingleOrDefault();
                if (result is null)
                    throw new ApplicationException($"Location query {place.SearchQuery} yielded no results.");
                return result;
            }, cancellationToken: cancellationToken));

            if (result.IsFailed) return Result.Fail(result.Errors);
            PopulatePlace(place, result.Value);
        }

        return Result.Ok(changes.AsEnumerable());
    }
}