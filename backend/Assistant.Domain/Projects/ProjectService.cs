using Assistant.Domain.Paginations;
using FluentResults;

namespace Assistant.Domain.Projects;

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