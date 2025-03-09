using Assistant.Domain.Projects;

namespace Assistant.WebApp.Infrastructure.OpenAi;

public class OrderedChange<TProject, TChange> where TChange : IChange<TProject>
{
    public required int Order { get; init; }
    public required TChange Change { get; init; }
}

public static class OrderedChangeExtensions
{
    public static IEnumerable<OrderedChange<TProject, IChange<TProject>>> ToGeneric<TProject, TChange>(
        this IEnumerable<OrderedChange<TProject, TChange>> source) where TChange : IChange<TProject>
    {
        foreach (var element in source)
        {
            yield return new OrderedChange<TProject, IChange<TProject>>
            {
                Order = element.Order,
                Change = element.Change
            };
        }
    }
}