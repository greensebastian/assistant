using FluentResults;

namespace Assistant.Domain.Projects;

public interface IChangeProvider<in TProject, TChange> where TChange : IChange<TProject>
{
    public Task<Result<IEnumerable<TChange>>> GetChangeSuggestions(TProject project, string prompt,
        CancellationToken cancellationToken);
}