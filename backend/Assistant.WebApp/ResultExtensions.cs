using FluentResults;

namespace Assistant.WebApp;

public static class ResultExtensions
{
    public static string LineSeparatedErrors(this Result result) => string.Join("\n", result.Errors);
    
    public static string LineSeparatedErrors<T>(this Result<T> result) => string.Join("\n", result.Errors);
}