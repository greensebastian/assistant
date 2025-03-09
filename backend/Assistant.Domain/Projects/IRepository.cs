using Assistant.Domain.Paginations;
using FluentResults;

namespace Assistant.Domain.Projects;

public interface IRepository<TProject, TMeta, TItem> where TProject : Project<TMeta, TItem> where TItem : ProjectItem
{
    public Task<Result> Save(CancellationToken cancellationToken);

    public Result Add(TProject project);

    public Task<Result<Paginated<TProject>>> Get(PaginationRequest pagination, CancellationToken cancellationToken);

    public Task<Result<TProject>> Get(Guid projectId, CancellationToken cancellationToken);

    public Task<Result> Delete(Guid projectId, CancellationToken cancellationToken);
}