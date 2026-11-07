namespace Decentraverse.Generator;

// Tutte le grandezze sono interi a virgola fissa, così l'output (e gli snapshot JSON)
// non dipendono mai dal floating point. Unità:
//   distanze planetarie → micro-UA (1_000_000 = 1 UA)
//   massa stellare      → milli-masse solari (1000 = il Sole)
//   luminosità          → micro-luminosità solari (1_000_000 = il Sole)
//   raggio planetario   → milli-raggi terrestri (1000 = la Terra)
//   lune                → chilometri

public enum StarKind { Star, BlackHole }

public enum SpectralClass { O, B, A, F, G, K, M }

public enum PlanetType { Rocky, Gas, Ice, Lava }

/// <summary>Posizione rispetto alla zona abitabile (HZ).</summary>
public enum OrbitZone
{
    /// <summary>Più vicino di metà del bordo interno della HZ.</summary>
    Scorched,
    /// <summary>Tra metà del bordo interno e il bordo interno.</summary>
    Hot,
    Habitable,
    /// <summary>Oltre la HZ, fino al doppio del bordo esterno (linea del gelo).</summary>
    Cold,
    /// <summary>Oltre la linea del gelo.</summary>
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
