using FluentResults;
using NodaTime;

namespace ItineraryManager.Domain.Itineraries;

public class Itinerary
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public List<Activity> Activities { get; set; } = new();

    private Result Remove(string activityId) => Activities.RemoveAll(activity => activity.Id == activityId) > 0
        ? Result.Ok()
        : Result.Fail("Activity to remove was not found");
    
    private Result Add(Activity activity, string? precedingActivityId)
    {
        var shouldHavePreceding = !string.IsNullOrWhiteSpace(precedingActivityId);
        var precedingActivity = shouldHavePreceding
            ? Activities.SingleOrDefault(a => a.Id == precedingActivityId)
            : null;
        if (shouldHavePreceding && precedingActivity is null)
        {
            return Result.Fail("Preceding activity was not found");
        }

        if (precedingActivity is null) Activities.Add(activity);
        else Activities.Insert(Activities.IndexOf(precedingActivity) + 1, activity);
        return Result.Ok();
    }

    private Result Reorder(string activityId, string? precedingActivityId = null)
    {
        var activity = Activities.SingleOrDefault(a => a.Id == activityId);
        
        if (activity is null) return Result.Fail("Activity to reorder was not found");
        
        Activities.Remove(activity);
        Add(activity, precedingActivityId);
        return Result.Ok();
    }

    public Result Apply(IItineraryChange change) => change.Apply(this);

    public record ActivityRemoval(string ActivityId) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary) => itinerary.Remove(ActivityId);
    }

    public record ActivityCreation(Activity Activity, string? PrecedingActivityId = null) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary) => itinerary.Add(Activity, PrecedingActivityId);
    }

    public record ActivityReordering(string ActivityId, string? PrecedingActivityId = null) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary) => itinerary.Reorder(ActivityId, PrecedingActivityId);
    }
}

public interface IItineraryChange
{
    public Result Apply(Itinerary itinerary);
}

public class Activity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required TimeAndPlace Start { get; set; }
    public required TimeAndPlace End { get; set; }
}

public class TimeAndPlace
{
    public required ZonedDateTime Time { get; set; }
    public required Place Place { get; set; }
}

public class Place
{
    public required string Reference { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}