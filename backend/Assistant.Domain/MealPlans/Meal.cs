using Assistant.Domain.Projects;

namespace Assistant.Domain.MealPlans;

public class Meal : ProjectItem
{
    public required string Recipe { get; set; }
    
    public required string RecipeLink { get; set; }
    
    public required DateTime EatOn { get; set; }
}