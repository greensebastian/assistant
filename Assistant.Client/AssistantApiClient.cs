using System.Net.Http.Json;
using Assistant.Api.Interface;

namespace Assistant.Client;

public class AssistantApiClient(HttpClient client) : IAssistantApi
{
    public async Task<PingResponse> Ping(PingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("/ping", request, cancellationToken: cancellationToken);
        var returned = await response.Content.ReadFromJsonAsync<PingResponse>(cancellationToken: cancellationToken);
        return returned ?? throw new ApplicationException("Invalid response from api");
    }
}
