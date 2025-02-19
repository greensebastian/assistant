using FluentResults;
using NodaTime;

namespace ItineraryManager.Domain.Itineraries;

public class Itinerary
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public List<Activity> Activities { get; init; } = new();

    public Result Remove(Activity activity) => Activities.Remove(activity)
        ? Result.Ok()
        : Result.Fail("Activity to remove was not found");
    
    public Result Add(Activity activity, Activity? previousActivity)
    {
        if (previousActivity is not null && !Activities.Contains(previousActivity))
        {
            return Result.Fail("Previous activity was not found");
        }

        if (previousActivity is null) Activities.Add(activity);
        else Activities.Insert(Activities.IndexOf(previousActivity) + 1, activity);
        return Result.Ok();
    }
}

public class Activity
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required TimeAndPlace Start { get; set; }
    public required TimeAndPlace End { get; set; }
}

public class TimeAndPlace
{
    public required Guid Id { get; init; }
    public required ZonedDateTime Time { get; set; }
    public required Place Place { get; set; }
}

public class Place
{
    public required Guid Id { get; init; }
    public required string Reference { get; init; }
    public required string Name { get; init; }
}