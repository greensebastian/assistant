using Assistant.Domain.MealPlans;
using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi.MealPlans;

public class MealPlanChangeRequestModel : IResponseModel<IChange<MealPlan>>
{
    public required List<OrderedChange<MealPlan, ItemAddition<MealPlan, string?, Meal>>> Creations { get; init; } = [];
    public required List<OrderedChange<MealPlan, ItemRemoval<MealPlan, string?, Meal>>> Removals { get; init; } = [];
    public required List<OrderedChange<MealPlan, ItemReordering<MealPlan, string?, Meal>>> Reorderings { get; init; } = [];
    public required string Reasoning { get; init; } = "";

    public IEnumerable<IChange<MealPlan>> GetChanges()
    {
        List<OrderedChange<MealPlan, IChange<MealPlan>>> results =
        [
            ..Creations.ToGeneric(),
            ..Removals.ToGeneric(),
            ..Reorderings.ToGeneric()
        ];

        return results.OrderBy(c => c.Order).Select(c => c.Change);
    }

    public string GetReasoning() => Reasoning;

    public static string GetSystemMessage() => """
                                               You are an assistant to create a meal plan for recipes on the web.
                                               The user provides a prompt, and based on that prompt, you will suggest which changes you would make to the provided meal plan to accomodate those prompts.
                                               The changes can be Creation, Removal, and Reordering of meals.
                                               Suggested changes include recipe links, which need to be valid links to the original recipe.
                                               Include the reasoning for the changes in a way that can be presented to the user.

                                               Model details:
                                               - All Id fields are unique numbers. When replacing a meal, the new meal should have a new Id
                                               - The "EatOn" property is a datetime in the users local timezone. Example: "2025-03-09T12:00:15"
                                               """;
}