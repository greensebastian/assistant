using Assistant.Domain.Projects;
using Assistant.WebApp.Infrastructure.OpenAi;

namespace Assistant.WebApp.Projects;

public static class WebApplicationExtensions
{
    public static ProjectBuilder<TProject, TMeta, TItem, TChange, TResponse> AddProjectType<TProject, TMeta, TItem, TChange, TResponse, TRepository>(this WebApplicationBuilder builder) where TProject : Project<TMeta, TItem> where TItem : ProjectItem where TChange : IChange<TProject> where TRepository : class, IRepository<TProject, TMeta, TItem> where TResponse : IResponseModel<TChange>
    {
        builder.Services.AddScoped<IRepository<TProject, TMeta, TItem>, TRepository>();
        builder.Services.AddScoped<ProjectService<TProject, TMeta, TItem, TChange>>();
        builder.ConfigureAndSnapshot<OpenAiSettings>("OpenAi");
        builder.Services.Configure<OpenAiSettings<TResponse>>(_ => { });
        builder.Services.AddSingleton<GenericOpenAiClient<TProject, TResponse, TChange>>();
        builder.Services.AddSingleton<ProjectChangeProvider<TProject, TChange, TResponse>>();
        builder.Services.AddSingleton<IChangeProvider<TProject, TChange>>(sp =>
            sp.GetRequiredService<ProjectChangeProvider<TProject, TChange, TResponse>>());

        return new ProjectBuilder<TProject, TMeta, TItem, TChange, TResponse>(builder);
    }
}