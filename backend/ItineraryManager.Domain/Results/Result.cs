namespace ItineraryManager.Domain.Results;

public class Result<T> : Result
{
    public T? Value { get; private init; }
    public override bool Success => Value is not null && Errors.Length == 0;
    
    public static Result<T> Ok(T value) => new(){ Value = value };

    public static async Task<Result<T>> Try(Func<Task<T>> action, Func<Exception, Task<Error[]>> errorFactory)
    {
        try
        {
            return Ok(await action());
        }
        catch (Exception e)
        {
            return new Result<T>
            {
                Errors = await errorFactory(e)
            };
        }
    }
    
    public static async Task<Result<T>> Try(Func<Task<T>> action, Func<Exception, Error[]> errorFactory)
    {
        try
        {
            return Ok(await action());
        }
        catch (Exception e)
        {
            return new Result<T>
            {
                Errors = errorFactory(e)
            };
        }
    }
}

public class Result
{
    protected Result() {}
    public Error[] Errors { get; protected init; } = [];
    public virtual bool Success => Errors.Length == 0;
    
    public static Result Ok() => new();

    public static Result Fail(string message, ErrorType type = ErrorType.Unknown) =>
        new() { Errors = [new Error(message, type)] };
    
    public static Result Fail(params Error[] errors) =>
        new() { Errors = errors };

    public static async Task Try(Func<Task> action, Func<Exception, Task<Error[]>> errorFactory)
    {
        try
        {
            await action();
            Ok();
        }
        catch (Exception e)
        {
            Fail(await errorFactory(e));
        }
    }
    
    public static async Task Try(Func<Task> action, Func<Exception, Error[]> errorFactory)
    {
        try
        {
            await action();
            Ok();
        }
        catch (Exception e)
        {
            Fail(errorFactory(e));
        }
    }
}

public record Error(string Message, ErrorType Type = ErrorType.Unknown);

public enum ErrorType
{
    Unknown = 0
}