using FluentResults;

namespace Assistant.Domain.Projects;

public record ItemRemoval<TProject, TMeta, TItem>(string ItemId) : IChange<TProject> where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem
{
    public Result Apply(TProject project)
    {
        return project.Items.RemoveAll(item => item.Id == ItemId) > 0
            ? Result.Ok()
            : Result.Fail("Item to remove was not found");
    }

    public string Description(TProject project)
    {
        var item = project.Items.SingleOrDefault(a => a.Id == ItemId);
        return item is null ? "Remove \"<Missing Item>\"." : $"Remove \"{project.Items.Single(a => a.Id == ItemId).Name}\".";
    }
}