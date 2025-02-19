using ItineraryManager.Domain.Paginations;
using ItineraryManager.Domain.Results;

namespace ItineraryManager.Domain.Itineraries;

public class ItineraryService(IItineraryRepository repository)
{
    public async Task<Result<Itinerary>> CreateItinerary(string name, CancellationToken cancellationToken)
    {
        var itinerary = new Itinerary
        {
            Id = Guid.NewGuid(),
            Name = name
        };
        return await Result<Itinerary>.Try(async () =>
        {
            repository.Add(itinerary);
            await repository.Save(cancellationToken);
            return itinerary;
        }, exception => [new(exception.Message)]);
    }

    public async Task<Result<Paginated<Itinerary>>> GetItineraries(PaginationRequest pagination, CancellationToken cancellationToken)
    {
        return await Result<Paginated<Itinerary>>.Try(async () =>
        {
            var response = await repository.Get(pagination, cancellationToken);
            return response.
        });
    }
}