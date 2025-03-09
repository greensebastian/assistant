using FluentResults;

namespace Assistant.Domain.Projects;

public record ItemAddition<TProject, TMeta, TItem>(TItem Item, string? PrecedingItemId = null) : IChange<TProject> where TProject : Project<TMeta, TItem> where TItem : ProjectItem
{
    public Result ApplyTo(TProject project)
    {
        var shouldHavePreceding = !string.IsNullOrWhiteSpace(PrecedingItemId);
        var precedingItem = shouldHavePreceding
            ? project.Items.SingleOrDefault(a => a.Id == PrecedingItemId)
            : null;
        if (shouldHavePreceding && precedingItem is null)
        {
            return Result.Fail("Preceding item was not found");
        }

        if (precedingItem is null) project.Items.Add(Item);
        else project.Items.Insert(project.Items.IndexOf(precedingItem) + 1, Item);
        return Result.Ok();
    }

    public string Description(TProject project)
    {
        var description =
            $"Add new item \"{Item.Name}\"";
        var precedingItem = PrecedingItemId is null
            ? null
            : project.Items.SingleOrDefault(a => a.Id == PrecedingItemId);

        return precedingItem is null
            ? description + " at the end."
            : description + $" after \"{precedingItem.Name}\".";
    }
}