namespace Assistant.Domain.Projects;

public abstract class Project<TMeta, TItem> where TItem : ProjectItem
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required TMeta Meta { get; set; }

    public List<TItem> Items { get; set; } = [];
}