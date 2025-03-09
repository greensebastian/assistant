using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi;

internal interface IChangeAdapter<TProject, out TChange> where TChange : IChange<TProject>
{
    public TChange GetChange();
}