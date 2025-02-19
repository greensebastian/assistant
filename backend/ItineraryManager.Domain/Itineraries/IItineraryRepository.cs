using ItineraryManager.Domain.Paginations;
using ItineraryManager.Domain.Results;

namespace ItineraryManager.Domain.Itineraries;

public interface IItineraryRepository
{
    public Task<Result> Save(CancellationToken cancellationToken);
    
    public Result<Itinerary> Add(Itinerary itinerary);
    
    public Task<Result<Paginated<Itinerary>>> Get(PaginationRequest pagination, CancellationToken cancellationToken);
}