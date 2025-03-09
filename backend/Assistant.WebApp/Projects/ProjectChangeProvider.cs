using Assistant.Domain.Projects;
using Assistant.WebApp.Infrastructure.OpenAi;
using FluentResults;

namespace Assistant.WebApp.Projects;

public class ProjectChangeProvider<TProject, TChange, TResponse>(GenericOpenAiClient<TProject, TResponse, TChange> openAiClient) : IChangeProvider<TProject, TChange> where TChange : IChange<TProject> where TResponse : IResponseModel<TChange>
{
    public async Task<Result<IEnumerable<TChange>>> GetChangeSuggestions(TProject project, string prompt, CancellationToken cancellationToken) => await openAiClient.GetChangeSuggestions(project, prompt, cancellationToken);
}