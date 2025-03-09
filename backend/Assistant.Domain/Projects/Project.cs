using FluentResults;

namespace Assistant.Domain.Projects;

public abstract class Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public TMeta Meta { get; set; } = new();

    public List<TItem> Items { get; set; } = [];

    public Result Apply(IChange<Project<TMeta, TItem>> change) => Result.Try(() =>
    {
        change.Apply(this);
    });
}