using FluentResults;
using ItineraryManager.Domain.Paginations;

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
        
        var addResult = repository.Add(itinerary);
        if (addResult.IsFailed)
            return Result.Fail(addResult.Errors);
        
        var saveResult = await repository.Save(cancellationToken);
        if (saveResult.IsFailed)
            return Result.Fail(saveResult.Errors);
        
        return Result.Ok(itinerary);
    }

    public async Task<Result<Paginated<Itinerary>>> GetItineraries(PaginationRequest pagination, CancellationToken cancellationToken)
    {
        return await repository.Get(pagination, cancellationToken);
    }
}