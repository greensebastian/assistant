using FluentResults;
using Assistant.Domain.Itineraries;
using Assistant.Domain.Paginations;
using Assistant.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace Assistant.WebApp.Infrastructure.Database;

public class ItineraryRepository(AssistantDbContext dbContext) : IRepository<Itinerary, object?, Activity>
{
    public async Task<Result> Save(CancellationToken cancellationToken)
    {
        var result = await Result.Try(() => dbContext.SaveChangesAsync(cancellationToken));
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }

    public Result Add(Itinerary itinerary)
    {
        var result = Result.Try(() => dbContext.Itineraries.Add(itinerary));
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }

    public async Task<Result<Paginated<Itinerary>>> Get(PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var countResult = await Result.Try(() => dbContext.Itineraries.CountAsync(cancellationToken));
        if (countResult.IsFailed) return Result.Fail(countResult.Errors);

        var result = await Result.Try(() => dbContext.Itineraries
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .ToArrayAsync(cancellationToken));
        if (result.IsFailed) return Result.Fail(result.Errors);

        return new Paginated<Itinerary>(result.Value,
            new PaginationResponse(pagination.Offset, pagination.Limit, countResult.Value));
    }

    public async Task<Result<Itinerary>> Get(Guid itineraryId, CancellationToken cancellationToken)
    {
        var result = await Result.Try(() => dbContext.Itineraries.SingleOrDefaultAsync(i => i.Id == itineraryId, cancellationToken));
        return result.Value is null ? Result.Fail("Itinerary not found") : result.Value;
    }

    public async Task<Result> Delete(Guid itineraryId, CancellationToken cancellationToken)
    {
        var itinerary = await Get(itineraryId, cancellationToken);
        if (itinerary.IsFailed) return Result.Fail(itinerary.Errors);
        var result = Result.Try(() => dbContext.Itineraries.Remove(itinerary.Value));
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }
}