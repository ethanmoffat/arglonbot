using CosineKitty;

namespace arglonbot;

public record EclipsePathPoint(DateTime TimeUtc, double Latitude, double Longitude);

public class EclipsePathResolver : IEclipsePathResolver
{
    private const double KmPerAu = 1.4959787069098932e+8;
    private const double EarthEquatorialKm = 6378.1366;

    private const double EarthFlattening = 0.996647180302104; // Ratio of Earth's polar to equatorial radius, used to treat the ellipsoid as a unit sphere.

    private static readonly TimeSpan SearchSpan = TimeSpan.FromHours(3);

    // ~10 minutes is roughly 280km of ground track - coarse enough to keep the sample list short, fine enough to name every region the path crosses.
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(10);

    public IReadOnlyList<EclipsePathPoint> TracePath(DateTime peakUtc)
    {
        var points = new List<EclipsePathPoint>();

        for (var offset = -SearchSpan; offset <= SearchSpan; offset += SampleInterval)
        {
            var time = peakUtc + offset;
            var location = ShadowAxisIntersection(new AstroTime(time));
            if (location != null)
            {
                points.Add(new EclipsePathPoint(time, location.Value.Latitude, location.Value.Longitude));
            }
        }

        return points;
    }

    private static (double Latitude, double Longitude)? ShadowAxisIntersection(AstroTime time)
    {
        var sun = ToScaledEarthRadii(Astronomy.GeoVector(Body.Sun, time, Aberration.Corrected));
        var moon = ToScaledEarthRadii(Astronomy.GeoMoon(time));

        var direction = (x: moon.x - sun.x, y: moon.y - sun.y, z: moon.z - sun.z); // Ray from the Sun through the Moon: p(u) = sun + u * direction

        // Scaling z by the flattening ratio turns the ellipsoid into a unit sphere, so the intersection reduces to a quadratic.
        var a = (direction.x * direction.x) + (direction.y * direction.y) + (direction.z * direction.z);
        var b = 2 * ((sun.x * direction.x) + (sun.y * direction.y) + (sun.z * direction.z));
        var c = (sun.x * sun.x) + (sun.y * sun.y) + (sun.z * sun.z) - 1.0;

        var discriminant = (b * b) - (4 * a * c);
        if (discriminant < 0)
        {
            return null;
        }

        var distance = (-b - Math.Sqrt(discriminant)) / (2 * a); // The smaller root is the near-side (daylight) intersection.
        var hit = (
            x: sun.x + (distance * direction.x),
            y: sun.y + (distance * direction.y),
            z: sun.z + (distance * direction.z));

        // Undo the z scaling and convert back to AU so the library can map it to a geographic point.
        var vector = new AstroVector(
            hit.x * EarthEquatorialKm / KmPerAu,
            hit.y * EarthEquatorialKm / KmPerAu,
            hit.z * EarthFlattening * EarthEquatorialKm / KmPerAu,
            time);

        var observer = Astronomy.VectorObserver(vector, EquatorEpoch.OfDate);
        return (observer.latitude, observer.longitude);
    }

    private static (double x, double y, double z) ToScaledEarthRadii(AstroVector vector) =>
    (
        vector.x * KmPerAu / EarthEquatorialKm,
        vector.y * KmPerAu / EarthEquatorialKm,
        vector.z * KmPerAu / EarthEquatorialKm / EarthFlattening
    );
}

public interface IEclipsePathResolver
{
    IReadOnlyList<EclipsePathPoint> TracePath(DateTime peakUtc);
}
