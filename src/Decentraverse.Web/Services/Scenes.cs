using Decentraverse.Generator;
using Decentraverse.Generator.Rendering;

namespace Decentraverse.Web.Services;

// Plain data sent to the JavaScript renderer. Short property names keep the interop payload small.

/// <summary>One star on the galaxy map.</summary>
public sealed record GalaxyStar(long N, double X, double Y, uint C, double S, int K, int D, string Name, string Cls);

public sealed record SceneStar(uint C, double S);

public sealed record ScenePlanet(int Index, string Name, uint C, double Orbit, double S, double Speed, double Phase, bool Ringed);

public sealed record SystemScene(string Name, bool BlackHole, IReadOnlyList<SceneStar> Stars, double HzInner, double HzOuter, IReadOnlyList<ScenePlanet> Planets);

public sealed record SceneMoon(double Orbit, double S, uint C, double Speed, double Phase);

/// <summary>Turns generated data into drawable scenes. Rendering only: floating point is fine here.</summary>
public static class SceneBuilder
{
    public const int KindStar = 0, KindBlackHole = 1, KindBinary = 2;

    public static GalaxyStar ToGalaxyStar(BlockInput block, StarSystem system, long referenceNumber)
    {
        var (x, y) = GalaxyLayout.Position(block.Number, block.Hash, referenceNumber);
        Star main = system.Stars[0];
        int kind = system.IsBlackHole ? KindBlackHole : system.IsBinary ? KindBinary : KindStar;
        return new GalaxyStar(block.Number, x, y, main.ColorRgb, GalaxyStarSize(main), kind, system.Density, system.Name, Labels.Class(main));
    }

    /// <summary>World-unit radius on the map: grows with the logarithm of the mass.</summary>
    public static double GalaxyStarSize(Star star) => star.Kind == StarKind.BlackHole
        ? 7
        : 2.2 + 1.6 * Math.Log10(star.MassMilliSolar / 80.0 + 1);

    /// <summary>
    /// Orbits span a factor of hundreds, so they are drawn on a square-root scale:
    /// the outermost planet sits at radius 400, the star at the centre.
    /// </summary>
    public static SystemScene ToSystemScene(StarSystem system)
    {
        long maxOrbit = system.Planets.Count > 0 ? system.Planets[^1].OrbitMicroAu : Math.Max(1, system.HabitableZoneOuterMicroAu * 2);
        double Scale(long microAu) => 50 + 350 * Math.Sqrt(microAu / (double)maxOrbit);

        var stars = system.Stars.Select(s => new SceneStar(s.ColorRgb, SystemStarSize(s))).ToArray();

        long innermost = system.Planets.Count > 0 ? system.Planets[0].OrbitMicroAu : 1;
        var planets = system.Planets.Select(p => new ScenePlanet(
            p.Index,
            p.Name,
            Labels.PlanetColor(p.Type),
            Scale(p.OrbitMicroAu),
            2.5 + 2.5 * Math.Sqrt(p.RadiusMilliEarth / 1000.0),
            // Kepler: angular speed ∝ a^-1.5. The innermost planet takes 8 seconds per orbit.
            2 * Math.PI / 8 * Math.Pow(innermost / (double)p.OrbitMicroAu, 1.5),
            // Starting angle: any fixed value works, derived from data so it is always the same.
            (p.OrbitMicroAu % 360) * Math.PI / 180,
            p.Type == PlanetType.Gas && p.RadiusMilliEarth > 8_000)).ToArray();

        return new SystemScene(system.Name, system.IsBlackHole, stars,
            system.IsBlackHole ? 0 : Scale(system.HabitableZoneInnerMicroAu),
            system.IsBlackHole ? 0 : Scale(system.HabitableZoneOuterMicroAu),
            planets);
    }

    public static IReadOnlyList<SceneMoon> ToSceneMoons(Planet planet, IReadOnlyList<Moon> moons, double planetSize) =>
        moons.Select((m, j) => new SceneMoon(
            planetSize + 5 + 3.5 * j,
            1.1 + 1.6 * Math.Sqrt(m.RadiusKm / 2_700.0),
            Labels.MoonColor(m.Type),
            2 * Math.PI / (2.5 + 1.2 * j),
            (m.OrbitKm % 360) * Math.PI / 180)).ToArray();

    private static double SystemStarSize(Star star) => star.Kind == StarKind.BlackHole
        ? 16
        : Math.Clamp(9 + 6 * Math.Log10(star.MassMilliSolar / 80.0 + 1), 9, 30);
}
