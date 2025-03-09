using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi;

internal class OrderedChangeAdapter<TProject, TChange, TChangeAdapter> where TChangeAdapter : IChangeAdapter<TProject, TChange> where TChange : IChange<TProject>
{
    public required int Order { get; init; }
    public required TChangeAdapter Change { get; init; }
}