namespace Decentraverse.Generator;

// All quantities are fixed-point integers, so the output (and the JSON snapshots)
// never depend on floating point. Units:
//   planetary distances → micro-AU (1_000_000 = 1 AU)
//   stellar mass        → milli solar masses (1000 = the Sun)
//   luminosity          → micro solar luminosities (1_000_000 = the Sun)
//   planetary radius    → milli Earth radii (1000 = the Earth)
//   moons               → kilometres

public enum StarKind { Star, BlackHole }

public enum SpectralClass { O, B, A, F, G, K, M }

public enum PlanetType { Rocky, Gas, Ice, Lava }

/// <summary>Position relative to the habitable zone (HZ).</summary>
public enum OrbitZone
{
    /// <summary>Closer than half the inner edge of the HZ.</summary>
    Scorched,
    /// <summary>Between half the inner edge and the inner edge.</summary>
    Hot,
    Habitable,
    /// <summary>Beyond the HZ, up to twice the outer edge (frost line).</summary>
    Cold,
    /// <summary>Beyond the frost line.</summary>
    Frozen,
}

public enum MoonType { Rocky, Icy, Volcanic }

public sealed record Star(
    string Designation,
    StarKind Kind,
    SpectralClass? Class,
    int MassMilliSolar,
    int TemperatureK,
    long LuminosityMicroSolar,
    uint ColorRgb);

public sealed record Planet(
    int Index,
    string Name,
    PlanetType Type,
    OrbitZone Zone,
    long OrbitMicroAu,
    int RadiusMilliEarth,
    int MoonCount);

public sealed record Moon(
    int Index,
    string Name,
    MoonType Type,
    long OrbitKm,
    int RadiusKm);

public sealed record StarSystem(
    string GeneratorVersion,
    long BlockNumber,
    string BlockHash,
    string Name,
    bool IsBlackHole,
    bool IsBinary,
    IReadOnlyList<Star> Stars,
    long BinarySeparationMicroAu,
    long HabitableZoneInnerMicroAu,
    long HabitableZoneOuterMicroAu,
    int Density,
    IReadOnlyList<Planet> Planets);
