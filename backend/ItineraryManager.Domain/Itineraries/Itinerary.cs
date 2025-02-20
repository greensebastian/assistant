using FluentResults;
using NodaTime;

namespace ItineraryManager.Domain.Itineraries;

public class Itinerary
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public List<Activity> Activities { get; set; } = new();

    public Result Apply(IItineraryChange change) => change.Apply(this);

    public record ActivityRemoval(string ActivityId) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary)
        {
            return itinerary.Activities.RemoveAll(activity => activity.Id == ActivityId) > 0
                ? Result.Ok()
                : Result.Fail("Activity to remove was not found");
        }

        public IEnumerable<Place> Places() => [];
    }

    public record ActivityCreation(Activity Activity, string? PrecedingActivityId = null) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary)
        {
            var shouldHavePreceding = !string.IsNullOrWhiteSpace(PrecedingActivityId);
            var precedingActivity = shouldHavePreceding
                ? itinerary.Activities.SingleOrDefault(a => a.Id == PrecedingActivityId)
                : null;
            if (shouldHavePreceding && precedingActivity is null)
            {
                return Result.Fail("Preceding activity was not found");
            }

            if (precedingActivity is null) itinerary.Activities.Add(Activity);
            else itinerary.Activities.Insert(itinerary.Activities.IndexOf(precedingActivity) + 1, Activity);
            return Result.Ok();
        }

        public IEnumerable<Place> Places()
        {
            yield return Activity.Start.Place;
            yield return Activity.End.Place;
        }
    }

    public record ActivityReordering(string ActivityId, string? PrecedingActivityId = null) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary)
        {
            var activity = itinerary.Activities.SingleOrDefault(a => a.Id == ActivityId);
            if (activity is null) return Result.Fail("Activity to reorder was not found");
        
            itinerary.Activities.Remove(activity);
            itinerary.Apply(new ActivityCreation(activity, PrecedingActivityId));
            return Result.Ok();
        }

        public IEnumerable<Place> Places() => [];
    }
    
    public record ActivityRescheduling(string ActivityId, ZonedDateTime NewStart, ZonedDateTime NewEnd) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary)
        {
            var activity = itinerary.Activities.SingleOrDefault(a => a.Id == ActivityId);
            if (activity is null) return Result.Fail("Activity to reschedule was not found");

            activity.Start.Time = NewStart;
            activity.End.Time = NewEnd;
            return Result.Ok();
        }

        public IEnumerable<Place> Places() => [];
    }
}

public interface IItineraryChange
{
    public Result Apply(Itinerary itinerary);

    public IEnumerable<Place> Places();
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
    public required string Uri { get; set; }
    public required string SearchQuery { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}