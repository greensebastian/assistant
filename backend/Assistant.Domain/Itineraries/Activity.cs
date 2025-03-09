using Assistant.Domain.Projects;

namespace Assistant.Domain.Itineraries;

public class Activity : ProjectItem
{
    public required string Description { get; set; }
    public required TimeAndPlace Start { get; set; }
    public required TimeAndPlace End { get; set; }
}