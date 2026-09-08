namespace arglonbot;

public interface IRegionDescriber
{
    string Describe(IReadOnlyList<EclipsePathPoint> points);
}

public class RegionDescriber : IRegionDescriber
{
    private record Region(string Name, double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude);

    // Coarse land bounding boxes. Order matters: boxes overlap, and the first match wins, so a
    // region must precede any wider box that would otherwise swallow it (Mexico before the US
    // boxes, Iceland before Greenland).
    private static readonly Region[] Regions =
    [
        new("Alaska", 54, 72, -170, -130),
        new("western Canada", 49, 70, -140, -95),
        new("central Canada", 49, 70, -95, -75),
        new("eastern Canada", 43, 62, -75, -52),
        new("Central America", 7, 18, -92, -77),
        new("Mexico", 14, 32.5, -118, -86),
        new("the western US", 32.5, 49, -125, -104),
        new("the central US", 25, 49, -104, -85),
        new("the eastern US", 25, 47, -85, -67),
        new("the Caribbean", 10, 27, -85, -59),
        new("South America", -56, 13, -82, -34),
        new("Iceland", 63, 67, -25, -13),
        new("Greenland", 59, 84, -73, -11),
        new("northern Europe", 54, 71, -11, 32),
        new("western Europe", 43, 55, -10, 8),
        new("southern Europe", 35, 48, -10, 30),
        new("eastern Europe", 44, 60, 22, 45),
        new("northern Africa", 15, 37, -18, 36),
        new("central Africa", -10, 16, -18, 45),
        new("southern Africa", -35, -10, 10, 42),
        new("the Middle East", 12, 42, 34, 63),
        new("Russia", 50, 78, 30, 180),
        new("central Asia", 35, 55, 46, 88),
        new("India", 7, 35, 68, 90),
        new("China", 20, 50, 88, 125),
        new("Southeast Asia", -11, 24, 92, 141),
        new("Japan", 30, 46, 129, 146),
        new("Australia", -44, -10, 112, 154),
        new("New Zealand", -48, -34, 166, 179),
        new("Antarctica", -90, -63, -180, 180),
    ];

    public string Describe(IReadOnlyList<EclipsePathPoint> points)
    {
        var names = new List<string>();

        foreach (var point in points)
        {
            var name = FindRegion(point.Latitude, point.Longitude);

            // Skip ocean, and collapse consecutive samples over the same region.
            if (name != null && (names.Count == 0 || names[^1] != name))
            {
                names.Add(name);
            }
        }

        return FormatList(names);
    }

    private static string? FindRegion(double latitude, double longitude) =>
        Regions.FirstOrDefault(x => latitude >= x.MinLatitude && latitude <= x.MaxLatitude
                                 && longitude >= x.MinLongitude && longitude <= x.MaxLongitude)?.Name;

    private static string FormatList(IReadOnlyList<string> names) => names.Count switch
    {
        0 => string.Empty,
        1 => names[0],
        2 => $"{names[0]} and {names[1]}",
        _ => $"{string.Join(", ", names.Take(names.Count - 1))}, and {names[^1]}",
    };
}
