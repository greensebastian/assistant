namespace Assistant.Api.Interface;

public interface IAssistantApi : IPingableService { }

public record PingRequest(string Message);

public record PingResponse(string Message);

public interface IPingableService
{
    public Task<PingResponse> Ping(PingRequest request, CancellationToken cancellationToken = default);
}
