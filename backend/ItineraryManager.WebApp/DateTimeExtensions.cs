using NodaTime;

namespace ItineraryManager.WebApp;

public static class DateTimeExtensions
{
    public static ZonedDateTime ToZonedDateTime(this DateTime dateTime, string tzId) => LocalDateTime
        .FromDateTime(dateTime)
        .InZoneStrictly(DateTimeZoneProviders.Tzdb.GetZoneOrNull(tzId)!);
}