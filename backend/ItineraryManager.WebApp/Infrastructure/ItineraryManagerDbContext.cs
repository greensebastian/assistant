using System.Globalization;
using ItineraryManager.Domain.Itineraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MongoDB.EntityFrameworkCore.Extensions;
using NodaTime;

namespace ItineraryManager.WebApp.Infrastructure;

public class ItineraryManagerDbContext(DbContextOptions<ItineraryManagerDbContext> options) : DbContext(options)
{
    public DbSet<Itinerary> Itineraries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Itinerary>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<Itinerary>().ToCollection("itineraries");
        
        modelBuilder.Entity<TimeAndPlace>()
            .Property(e => e.Time)
            .HasConversion(new ZonedDateTimeConverter());
    }
}

internal class ZonedDateTimeConverter() : ValueConverter<ZonedDateTime, string>(time => ToText(time), text => ToTime(text))
{
    private const string Separator = "||";
    private static string ToText(ZonedDateTime time) => $"{time.ToDateTimeUnspecified():O}{Separator}{time.Zone}";

    private static ZonedDateTime ToTime(string text)
    {
        var time = LocalDateTime.FromDateTime(DateTime.ParseExact(text.Split(Separator)[0], "O", CultureInfo.InvariantCulture));
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(text.Split(Separator)[1]);
        return time.InZoneStrictly(zone!);
    }
}