using NodaTime;

namespace Assistant.Domain.Itineraries;

public class TimeAndPlace
{
    public required ZonedDateTime Time { get; set; }
    public required Place Place { get; set; }
}