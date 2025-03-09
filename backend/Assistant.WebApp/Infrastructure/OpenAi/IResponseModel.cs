namespace Assistant.WebApp.Infrastructure.OpenAi;

public interface IResponseModel<out TChange>
{
    public IEnumerable<TChange> GetChanges();

    public string GetReasoning();

    public static abstract string GetSystemMessage();
}