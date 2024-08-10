namespace Assistant.Api.Interface;

public interface IAssistantApi : IPingableService, ICompletionService { }

public record PingRequest(string Message);

public record PingResponse(string Message);

public interface IPingableService
{
    public Task<PingResponse> Ping(PingRequest request, CancellationToken cancellationToken = default);
}

public record CompleteRequest(string Query);

public record CompleteResponse(string Message);

public interface ICompletionService
{
    public Task<CompleteResponse> Complete(CompleteRequest request, CancellationToken cancellationToken = default);
}
