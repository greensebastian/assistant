using System.Globalization;
using Assistant.Domain.Itineraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MongoDB.EntityFrameworkCore.Extensions;
using NodaTime;

namespace Assistant.WebApp.Infrastructure.Database;

public class AssistantDbContext(DbContextOptions<AssistantDbContext> options) : DbContext(options)
{
    public DbSet<Itinerary> Itineraries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var itineraryModel = modelBuilder.Entity<Itinerary>();

        itineraryModel.HasKey(e => e.Id);
        itineraryModel.ToCollection("itineraries");
        itineraryModel.OwnsMany(e => e.Items, activityModel =>
        {
            activityModel.OwnsOne(a => a.Start, start =>
            {
                start.Property(s => s.Time).HasConversion(new ZonedDateTimeConverter());
                start.OwnsOne(s => s.Place);
            });

            activityModel.OwnsOne(a => a.End, end =>
            {
                end.Property(e => e.Time).HasConversion(new ZonedDateTimeConverter());
                end.OwnsOne(e => e.Place);
            });
        });
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