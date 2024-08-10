using System.Net.Http.Json;
using Assistant.Api.Interface;

namespace Assistant.Client;

public class AssistantApiClient(HttpClient client) : IAssistantApi
{
    public async Task<PingResponse> Ping(PingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("/ping", request, cancellationToken);
        var returned = await response.Content.ReadFromJsonAsync<PingResponse>(cancellationToken);
        return returned ?? throw new ApplicationException("Invalid response from api");
    }

    public async Task<CompleteResponse> Complete(CompleteRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("/complete", request, cancellationToken);
        var returned = await response.Content.ReadFromJsonAsync<CompleteResponse>(cancellationToken);
        return returned ?? throw new ApplicationException("Invalid response from api");
    }
}
