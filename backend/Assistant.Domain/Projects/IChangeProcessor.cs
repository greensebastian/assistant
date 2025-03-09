using FluentResults;

namespace Assistant.Domain.Projects;

public interface IChangeProcessor<in TProject, TChange> where TChange : IChange<TProject>
{
    public Task<Result<IEnumerable<TChange>>> Process(TProject project, IReadOnlyList<TChange> changes,
        CancellationToken cancellationToken);
}