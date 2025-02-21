using System.Globalization;
using FluentResults;
using NodaTime;

namespace ItineraryManager.Domain.Itineraries;

public class Itinerary
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }

    private List<Activity> ActivitiesBacking { get; set; } = new();
    public IReadOnlyList<Activity> Activities => ActivitiesBacking;

    public Result Apply(IItineraryChange change) => Result.Try(() =>
    {
        change.Apply(this);
    });

    public record ActivityRemoval(string ActivityId) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary)
        {
            return itinerary.ActivitiesBacking.RemoveAll(activity => activity.Id == ActivityId) > 0
                ? Result.Ok()
                : Result.Fail("Activity to remove was not found");
        }

        public IEnumerable<Place> Places() => [];

        public string Description(Itinerary itinerary)
        {
            var activity = itinerary.Activities.SingleOrDefault(a => a.Id == ActivityId);
            return activity is null ? "Remove \"<Missing Activity>\"." : $"Remove \"{itinerary.Activities.Single(a => a.Id == ActivityId).Name}\".";
            
        }
            
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

            if (precedingActivity is null) itinerary.ActivitiesBacking.Add(Activity);
            else itinerary.ActivitiesBacking.Insert(itinerary.ActivitiesBacking.IndexOf(precedingActivity) + 1, Activity);
            return Result.Ok();
        }

        public IEnumerable<Place> Places()
        {
            yield return Activity.Start.Place;
            yield return Activity.End.Place;
        }

        public string Description(Itinerary itinerary)
        {
            var description =
                $"Add new activity \"{Activity.Name}\" starting on {Activity.Start.Time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} at {Activity.Start.Place.Name}";
            var precedingActivity = PrecedingActivityId is null
                ? null
                : itinerary.Activities.SingleOrDefault(a => a.Id == PrecedingActivityId);

            return precedingActivity is null
                ? description + " at the end."
                : description + $" after \"{precedingActivity.Name}\".";
        }
    }

    public record ActivityReordering(string ActivityId, string? PrecedingActivityId = null) : IItineraryChange
    {
        public Result Apply(Itinerary itinerary)
        {
            var activity = itinerary.Activities.SingleOrDefault(a => a.Id == ActivityId);
            if (activity is null) return Result.Fail("Activity to reorder was not found");
        
            itinerary.ActivitiesBacking.Remove(activity);
            itinerary.Apply(new ActivityCreation(activity, PrecedingActivityId));
            return Result.Ok();
        }

        public IEnumerable<Place> Places() => [];

        public string Description(Itinerary itinerary)
        {
            var activity = itinerary.Activities.Single(a => a.Id == ActivityId);
            var precedingActivity = PrecedingActivityId is null
                ? null
                : itinerary.Activities.SingleOrDefault(a => a.Id == PrecedingActivityId);

            return precedingActivity is null
                ? $"Move \"{activity.Name}\" to end"
                : $"Move \"{activity.Name}\" to after \"{precedingActivity.Name}\"";
        }
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

        public string Description(Itinerary itinerary){
            var activity = itinerary.Activities.SingleOrDefault(a => a.Id == ActivityId);

            return activity is null
                ? "Reschedule \"<Missing Activity>\"."
                : $"Reschedule \"{activity.Name}\" to {NewStart.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} to {NewEnd.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}.";
        }
    }
}

public interface IItineraryChange
{
    public Result Apply(Itinerary itinerary);

    public IEnumerable<Place> Places();

    public string Description(Itinerary itinerary);
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
    public required float Latitude { get; set; }
    public required float Longitude { get; set; }
}