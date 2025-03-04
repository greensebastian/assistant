using Assistant.Domain.Paginations;
using FluentResults;

namespace Assistant.Domain.Projects;

public abstract class ProjectItem
{
    public required string Id { get; set; }
    
    public required string Name { get; set; }
}

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

public interface IChange<in TProject>
{
    public Result Apply(TProject project);
        
    public string Description(TProject project);
}

public interface IChangeProvider<in TProject, TChange> where TChange : IChange<TProject>
{
    public Task<Result<IEnumerable<TChange>>> GetChangeSuggestions(TProject project, string prompt,
        CancellationToken cancellationToken);
}

public interface IChangeProcessor<in TProject, TChange> where TChange : IChange<TProject>
{
    public Task<Result<IEnumerable<TChange>>> Process(TProject project, IReadOnlyList<TChange> changes,
        CancellationToken cancellationToken);
}

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

public record ItemAddition<TProject, TMeta, TItem>(TItem Item, string? PrecedingItemId = null) : IChange<TProject> where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem
{
    public Result Apply(TProject project)
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

public record ItemReordering<TProject, TMeta, TItem>(string ItemId, string? PrecedingItemId = null) : IChange<TProject> where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem
{
    public Result Apply(TProject project)
    {
        var item = project.Items.SingleOrDefault(a => a.Id == ItemId);
        if (item is null) return Result.Fail("Item to reorder was not found");
    
        project.Items.Remove(item);
        project.Apply(new ItemAddition<Project<TMeta, TItem>, TMeta, TItem>(item, PrecedingItemId));
        return Result.Ok();
    }

    public string Description(TProject project)
    {
        var activity = project.Items.Single(a => a.Id == ItemId);
        var precedingActivity = PrecedingItemId is null
            ? null
            : project.Items.SingleOrDefault(a => a.Id == PrecedingItemId);

        return precedingActivity is null
            ? $"Move \"{activity.Name}\" to end"
            : $"Move \"{activity.Name}\" to after \"{precedingActivity.Name}\"";
    }
}

public class ProjectService<TProject, TMeta, TItem, TChange>(IRepository<TProject, TMeta, TItem> repository, IChangeProvider<TProject, TChange> changeProvider, IEnumerable<IChangeProcessor<TProject, TChange>> changeProcessors) where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem where TChange : IChange<TProject>
{
    public async Task<Result<TProject>> Create(TProject project, CancellationToken cancellationToken)
    {
        var addResult = repository.Add(project);
        if (addResult.IsFailed)
            return addResult;
        
        var saveResult = await repository.Save(cancellationToken);
        return saveResult.IsFailed ? saveResult : Result.Ok(project);
    }

    public async Task<Result<Paginated<TProject>>> Get(PaginationRequest pagination, CancellationToken cancellationToken) => await repository.Get(pagination, cancellationToken);

    public async Task<Result<TProject>> Get(Guid itineraryId, CancellationToken cancellationToken) => await repository.Get(itineraryId, cancellationToken);

    public async Task<Result<IEnumerable<TChange>>> GetChangeSuggestions(Guid projectId, string prompt,
        CancellationToken cancellationToken)
    {
        var projectResult = await repository.Get(projectId, cancellationToken);
        if (projectResult.IsFailed) return Result.Fail(projectResult.Errors);
        var project = projectResult.Value;

        var getChangesResult = await changeProvider.GetChangeSuggestions(projectResult.Value, prompt, cancellationToken);
        if (getChangesResult.IsFailed) return Result.Fail(getChangesResult.Errors);
        var changes = getChangesResult.Value.ToArray();

        foreach (var changeProcessor in changeProcessors)
        {
            var result = await changeProcessor.Process(project, changes, cancellationToken);
            if (result.IsFailed) return Result.Fail(result.Errors);
            changes = result.Value.ToArray();
        }
        
        return changes;
    }
    
    public async Task<Result<TProject>> ApplyChanges(Guid projectId, IEnumerable<TChange> changes, CancellationToken cancellationToken)
    {
        var project = await repository.Get(projectId, cancellationToken);
        if (project.IsFailed) return project;
        var itinerary = project.Value;
        foreach (var change in changes)
        {
            var result = itinerary.Apply((IChange<Project<TMeta, TItem>>)change);
            if (result.IsFailed) return result;
        }

        var saveResult = await repository.Save(cancellationToken);
        if (saveResult.IsFailed) return saveResult;
        return Result.Ok(itinerary);
    }

    public async Task<Result> Delete(Guid projectId, CancellationToken cancellationToken)
    {
        var deleteResult = await repository.Delete(projectId, cancellationToken);
        if (deleteResult.IsFailed) return deleteResult;

        return await repository.Save(cancellationToken);
    }
}

public interface IRepository<TProject, TMeta, TItem> where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem
{
    public Task<Result> Save(CancellationToken cancellationToken);
        
    public Result Add(TProject project);
        
    public Task<Result<Paginated<TProject>>> Get(PaginationRequest pagination, CancellationToken cancellationToken);
        
    public Task<Result<TProject>> Get(Guid projectId, CancellationToken cancellationToken);

    public Task<Result> Delete(Guid projectId, CancellationToken cancellationToken);
}