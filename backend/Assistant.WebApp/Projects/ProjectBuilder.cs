using Assistant.Domain.Projects;

namespace Assistant.WebApp.Projects;

public class ProjectBuilder<TProject, TMeta, TItem, TChange, TResponse>(WebApplicationBuilder builder)
    where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem where TChange : IChange<TProject>
{
    public ProjectBuilder<TProject, TMeta, TItem, TChange, TResponse> WithProcessor<TProcessor>() where TProcessor : class, IChangeProcessor<TProject, TChange>
    {
        builder.Services.AddSingleton<IChangeProcessor<TProject, TChange>, TProcessor>();
        return this;
    }
}