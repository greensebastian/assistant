using FluentResults;
using ItineraryManager.Domain.Itineraries.Dependencies;
using ItineraryManager.Domain.Paginations;

namespace ItineraryManager.Domain.Itineraries;

public class ItineraryService(IItineraryRepository repository, IItineraryChangeProvider changeProvider)
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
            return addResult;
        
        var saveResult = await repository.Save(cancellationToken);
        if (saveResult.IsFailed)
            return saveResult;
        
        return Result.Ok(itinerary);
    }

    public async Task<Result<Paginated<Itinerary>>> GetItineraries(PaginationRequest pagination, CancellationToken cancellationToken)
    {
        return await repository.Get(pagination, cancellationToken);
    }

    public async Task<Result<IEnumerable<IItineraryChange>>> RequestChangeSuggestions(Guid itineraryId, string prompt,
        CancellationToken cancellationToken)
    {
        var itineraryResult = await repository.Get(itineraryId, cancellationToken);
        if (itineraryResult.IsFailed) return Result.Fail(itineraryResult.Errors);

        var suggestedChangesResult = await changeProvider.RequestChangeSuggestions(itineraryResult.Value, prompt, cancellationToken);
        return suggestedChangesResult;
    }
    
    public async Task<Result<Itinerary>> ApplyChanges(Guid itineraryId, IEnumerable<IItineraryChange> changes, CancellationToken cancellationToken)
    {
        var itineraryResult = await repository.Get(itineraryId, cancellationToken);
        if (itineraryResult.IsFailed) return itineraryResult;
        var itinerary = itineraryResult.Value;
        foreach (var change in changes)
        {
            itinerary.Apply(change);
        }

        var saveResult = await repository.Save(cancellationToken);
        if (saveResult.IsFailed) return saveResult;
        return Result.Ok(itinerary);
    }
}