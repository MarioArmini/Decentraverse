namespace Decentraverse.Generator;

internal readonly record struct ClassSpec(
    SpectralClass Class,
    uint Weight,
    int MassMin, int MassMax,
    int TempMin, int TempMax,
    long LumMin, long LumMax,
    uint Color);

internal static class StarTables
{
    /// <summary>
    /// Distribuzione "giocabile" delle classi spettrali, pesi su 100.000.
    /// Masse in milli-masse solari, temperature in K, luminosità in micro-luminosità solari.
    /// I colori sono quelli convenzionali per classe (solo rendering).
    /// </summary>
    public static readonly ClassSpec[] Classes =
    [
        new(SpectralClass.O,     50, 16_000, 60_000, 30_000, 50_000, 30_000_000_000, 1_000_000_000_000, 0x9BB0FF),
        new(SpectralClass.B,    450,  2_100, 16_000, 10_000, 30_000,     25_000_000,    30_000_000_000, 0xAABFFF),
        new(SpectralClass.A,  2_000,  1_400,  2_100,  7_500, 10_000,      5_000_000,        25_000_000, 0xCAD7FF),
        new(SpectralClass.F,  4_500,  1_040,  1_400,  6_000,  7_500,      1_500_000,         5_000_000, 0xF8F7FF),
        new(SpectralClass.G,  9_000,    800,  1_040,  5_200,  6_000,        600_000,         1_500_000, 0xFFF4EA),
        new(SpectralClass.K, 14_000,    450,    800,  3_700,  5_200,         80_000,           600_000, 0xFFD2A1),
        new(SpectralClass.M, 70_000,     80,    450,  2_400,  3_700,            800,            80_000, 0xFFCC6F),
    ];

    public const uint TotalWeight = 100_000;

    /// <summary>
    /// Estrae una stella: 1) classe (NextBelow(100.000)), 2) posizione nella classe in per-mille (0..1000).
    /// La stessa posizione interpola massa, temperatura e luminosità, così restano coerenti tra loro.
    /// </summary>
    public static Star Draw(Xoshiro128StarStar rng, string designation)
    {
        uint r = rng.NextBelow(TotalWeight);
        ClassSpec spec = Classes[^1];
        foreach (var c in Classes)
        {
            if (r < c.Weight)
            {
                spec = c;
                break;
            }
            r -= c.Weight;
        }

        int f = rng.NextInt(0, 1001);
        return new Star(
            designation,
            StarKind.Star,
            spec.Class,
            (int)IntMath.Lerp(spec.MassMin, spec.MassMax, f),
            (int)IntMath.Lerp(spec.TempMin, spec.TempMax, f),
            IntMath.Lerp(spec.LumMin, spec.LumMax, f),
            spec.Color);
    }

    /// <summary>
    /// Bordi della zona abitabile in micro-UA, dalla luminosità L (micro-luminosità solari):
    /// interno = √(L/1,1) UA, esterno = √(L/0,53) UA, calcolati con interi:
    /// d[µUA] = 1000·√(L[µL☉]) / √1,1 = √(L·10⁶·10/11).
    /// </summary>
    public static long HabitableInnerMicroAu(long luminosityMicroSolar) =>
        (long)IntMath.ISqrt((ulong)luminosityMicroSolar * 1_000_000UL / 11UL * 10UL);

    public static long HabitableOuterMicroAu(long luminosityMicroSolar) =>
        (long)IntMath.ISqrt((ulong)luminosityMicroSolar * 1_000_000UL / 53UL * 100UL);

    /// <summary>Bordo interno della HZ del Sole (≈ 953.462 µUA): riferimento per scalare le orbite.</summary>
    public static readonly long SunHabitableInnerMicroAu = HabitableInnerMicroAu(1_000_000);
}
