using Assistant.Domain.Projects;
using NodaTime;

namespace Assistant.Domain.MealPlans;

public class Meal : ProjectItem
{
    public required string Recipe { get; set; }
    
    public required string RecipeLink { get; set; }
    
    public required LocalDateTime EatOn { get; set; }
}