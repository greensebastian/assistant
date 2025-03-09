namespace Assistant.Domain.Projects;

public abstract class ProjectItem
{
    public required string Id { get; set; }

    public required string Name { get; set; }
}