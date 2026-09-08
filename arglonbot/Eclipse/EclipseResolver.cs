using CosineKitty;

using static arglonbot.Configuration.MessageInfo;

namespace arglonbot;

public record EclipseDetails(EclipseEventType Type, DateTime PeakUtc, string Location, string MapUrl, EclipseKind Kind);

public class EclipseResolver : IEclipseResolver
{
    private const int MaxEclipsesToSearch = 4;

    private static readonly TimeSpan LookAheadWindow = TimeSpan.FromHours(24);

    private readonly IEclipsePathResolver _pathResolver;
    private readonly IRegionDescriber _regionDescriber;

    public EclipseResolver(IEclipsePathResolver pathResolver, IRegionDescriber regionDescriber)
    {
        _pathResolver = pathResolver;
        _regionDescriber = regionDescriber;
    }

    public EclipseDetails? Resolve(EclipseEventType type, DateTime time)
    {
        // Eclipse times are UTC, so normalize the caller's local notification time before comparing.
        var windowStart = time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime();
        var windowEnd = windowStart + LookAheadWindow;

        return type switch
        {
            EclipseEventType.Solar => ResolveSolar(windowStart, windowEnd),
            EclipseEventType.Lunar => ResolveLunar(windowStart, windowEnd),
            _ => null,
        };
    }

    private EclipseDetails? ResolveSolar(DateTime windowStart, DateTime windowEnd)
    {
        foreach (var eclipse in Astronomy.GlobalSolarEclipsesAfter(new AstroTime(windowStart)).Take(MaxEclipsesToSearch))
        {
            var peak = eclipse.peak.ToUtcDateTime();
            if (peak >= windowEnd)
            {
                break;
            }

            if (peak >= windowStart && eclipse.kind is EclipseKind.Total or EclipseKind.Annular)
            {
                return BuildSolarDetails(eclipse, peak);
            }
        }

        return null;
    }

    private EclipseDetails? ResolveLunar(DateTime windowStart, DateTime windowEnd)
    {
        foreach (var eclipse in Astronomy.LunarEclipsesAfter(new AstroTime(windowStart)).Take(MaxEclipsesToSearch))
        {
            var peak = eclipse.peak.ToUtcDateTime();
            if (peak >= windowEnd)
            {
                break;
            }

            if (peak >= windowStart && eclipse.kind == EclipseKind.Total)
            {
                return new EclipseDetails(EclipseEventType.Lunar, peak, "the entire night side of Earth", string.Empty, eclipse.kind);
            }
        }

        return null;
    }

    private EclipseDetails BuildSolarDetails(GlobalSolarEclipseInfo eclipse, DateTime peak)
    {
        var path = _pathResolver.TracePath(peak);
        var regions = _regionDescriber.Describe(path);

        var location = string.IsNullOrEmpty(regions)
            ? FormatCoordinates(eclipse.latitude, eclipse.longitude)
            : regions;

        return new EclipseDetails(EclipseEventType.Solar, peak, location, MapUrl(eclipse.latitude, eclipse.longitude), eclipse.kind);
    }

    private static string FormatCoordinates(double latitude, double longitude)
    {
        var latHemisphere = latitude >= 0 ? "N" : "S";
        var lonHemisphere = longitude >= 0 ? "E" : "W";

        return $"{Math.Abs(latitude):0.#}\u00B0{latHemisphere} {Math.Abs(longitude):0.#}\u00B0{lonHemisphere}";
    }

    private static string MapUrl(double latitude, double longitude) =>
        FormattableString.Invariant($"https://www.openstreetmap.org/?mlat={latitude:0.####}&mlon={longitude:0.####}#map=4/{latitude:0.####}/{longitude:0.####}");
}

public interface IEclipseResolver
{
    EclipseDetails? Resolve(EclipseEventType type, DateTime time);
}
