using FluentResults;
using Assistant.Domain.MealPlans;
using Assistant.Domain.Paginations;
using Assistant.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace Assistant.WebApp.Infrastructure.Database;

public class MealPlanRepository(AssistantDbContext dbContext) : IRepository<MealPlan, string?, Meal>
{
    public async Task<Result> Save(CancellationToken cancellationToken)
    {
        var result = await Result.Try(() => dbContext.SaveChangesAsync(cancellationToken));
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }

    public Result Add(MealPlan project)
    {
        var result = Result.Try(() => dbContext.MealPlans.Add(project));
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }

    public async Task<Result<Paginated<MealPlan>>> Get(PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var countResult = await Result.Try(() => dbContext.MealPlans.CountAsync(cancellationToken));
        if (countResult.IsFailed) return Result.Fail(countResult.Errors);

        var result = await Result.Try(() => dbContext.MealPlans
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .ToArrayAsync(cancellationToken));
        if (result.IsFailed) return Result.Fail(result.Errors);

        return new Paginated<MealPlan>(result.Value,
            new PaginationResponse(pagination.Offset, pagination.Limit, countResult.Value));
    }

    public async Task<Result<MealPlan>> Get(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await Result.Try(() => dbContext.MealPlans.SingleOrDefaultAsync(i => i.Id == projectId, cancellationToken));
        return result.Value is null ? Result.Fail("Meal plan not found") : result.Value;
    }

    public async Task<Result> Delete(Guid projectId, CancellationToken cancellationToken)
    {
        var mealPlan = await Get(projectId, cancellationToken);
        if (mealPlan.IsFailed) return Result.Fail(mealPlan.Errors);
        var result = Result.Try(() => dbContext.MealPlans.Remove(mealPlan.Value));
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }
}