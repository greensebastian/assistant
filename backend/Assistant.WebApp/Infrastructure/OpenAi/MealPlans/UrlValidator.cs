using Assistant.Domain.MealPlans;
using Assistant.Domain.Projects;
using FluentResults;

namespace Assistant.WebApp.Infrastructure.OpenAi.MealPlans;

public class UrlValidator(HttpClient httpClient) : IChangeProcessor<MealPlan, IChange<MealPlan>>
{
    public async Task<Result<IEnumerable<IChange<MealPlan>>>> Process(MealPlan project, IReadOnlyList<IChange<MealPlan>> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            if (change is ItemAddition<MealPlan, string?, Meal> addition)
            {
                var url = addition.Item.RecipeLink;
                var getRecipeResponse = await httpClient.GetAsync(url, cancellationToken);
                if ((int)getRecipeResponse.StatusCode is < 200 or >= 300)
                    return Result.Fail(
                        $"Status code does not indicate valid link {url}, {getRecipeResponse.StatusCode}");
            }
        }

        return Result.Ok(changes.AsEnumerable());
    }
}