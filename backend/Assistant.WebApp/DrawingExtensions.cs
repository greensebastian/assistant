using System.Drawing;
using Assistant.Domain.Itineraries;

namespace Assistant.WebApp;

public static class DrawingExtensions
{
    public static PointF PointOnPathTo(this PointF here, PointF there, float fraction = 0.5f)
    {
        var lat = here.X + (there.X - here.X) * fraction;
        var lon = here.Y + (there.Y - here.Y) * fraction;
        return new PointF(lat, lon);
    }

    public static PointF AsPointF(this Place place) => new PointF(place.Latitude, place.Longitude);

    public static PointF RotateCounterClockwise(this PointF point, float degrees)
    {
        var radians = degrees * (float)(Math.PI / 180.0);
        var cos = (float)Math.Cos(radians);
        var sin = (float)Math.Sin(radians);

        var newX = point.X * cos - point.Y * sin;
        var newY = point.X * sin + point.Y * cos;

        return new PointF(newX, newY);
    }

    public static PointF To(this PointF here, PointF there) => new(there.X - here.X, there.Y - here.Y);

    public static PointF Add(this PointF here, PointF other) => new(here.X + other.X, here.Y + other.Y);
}