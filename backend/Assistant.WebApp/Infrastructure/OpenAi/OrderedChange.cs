using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi;

internal class OrderedChange<TProject, TChange> where TChange : IChange<TProject>
{
    public required int Order { get; init; }
    public required TChange Change { get; init; }
}