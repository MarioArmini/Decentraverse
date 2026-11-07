namespace Decentraverse.Generator;

/// <summary>
/// Generator entry point. Pure functions: same block → same output, always.
///
/// WARNING: the order of PRNG draws is part of the rules.
/// Moving, adding or removing a single draw changes the universe:
/// in that case <see cref="GeneratorInfo.Version"/> must be bumped.
/// </summary>
public static class SystemGenerator
{
    /// <summary>Density scale: gas used and transactions above these thresholds count as "full".</summary>
    public const long DensityGasScale = 60_000_000;
    public const int DensityTxScale = 500;

    private static readonly (int Count, uint Weight)[] PlanetCounts =
    [
        (0, 8), (1, 8), (2, 10), (3, 12), (4, 13), (5, 13), (6, 12), (7, 9), (8, 7), (9, 5), (10, 3),
    ];

    public static StarSystem GenerateSystem(BlockInput block)
    {
        ArgumentNullException.ThrowIfNull(block);
        string hash = BlockHash.Normalize(block.Hash);
        int density = ComputeDensity(block.TxCount, block.GasUsed);

        // 1) Rare events: from the "rare" seed, independent of the PRNG.
        byte[] rare = Seeds.Rare(hash);
        bool isBlackHole = RareEvents.IsBlackHole(rare);
        bool isBinary = !isBlackHole && RareEvents.IsBinary(rare);

        // 2) Star(s) and system structure: from the "star" seed.
        var rng = Xoshiro128StarStar.FromSeed(Seeds.Star(hash));
        string name = NameGenerator.Generate(rng);

        if (isBlackHole)
        {
            // v1: a black hole has no planets (only the accretion disk, which is rendering).
            var hole = new Star("A", StarKind.BlackHole, null, rng.NextInt(5_000, 30_001), 0, 0, 0x000000);
            return new StarSystem(GeneratorInfo.Version, block.Number, hash, name,
                IsBlackHole: true, IsBinary: false, [hole], 0, 0, 0, density, []);
        }

        Star primary = StarTables.Draw(rng, "A");
        var stars = new List<Star> { primary };
        long luminosity = primary.LuminosityMicroSolar;
        int separationPercent = 0;

        if (isBinary)
        {
            Star companion = StarTables.Draw(rng, "B");
            if (companion.MassMilliSolar > primary.MassMilliSolar)
            {
                // Star A is always the more massive one.
                (primary, companion) = (companion with { Designation = "A" }, primary with { Designation = "B" });
            }
            stars = [primary, companion];
            luminosity = primary.LuminosityMicroSolar + companion.LuminosityMicroSolar;
            separationPercent = rng.NextInt(5, 41);
        }

        long hzInner = StarTables.HabitableInnerMicroAu(luminosity);
        long hzOuter = StarTables.HabitableOuterMicroAu(luminosity);

        // Close binary: planets orbit both stars (circumbinary) and
        // none may lie within 3 times the separation: all orbits are shifted outwards.
        long separation = hzInner * separationPercent / 100;
        long orbitOffset = 3 * separation;

        int planetCount = IntMath.Pick<int>(rng, PlanetCounts);

        // 3) Planets: each from its own seed.
        var planets = new Planet[planetCount];
        for (int i = 0; i < planetCount; i++)
        {
            planets[i] = GeneratePlanet(hash, i, name, hzInner, hzOuter, orbitOffset);
        }

        return new StarSystem(GeneratorInfo.Version, block.Number, hash, name,
            IsBlackHole: false, isBinary, stars, separation, hzInner, hzOuter, density, planets);
    }

    /// <summary>Moons of planet <paramref name="planetIndex"/>: generated only on demand, each from its own seed.</summary>
    public static IReadOnlyList<Moon> GenerateMoons(BlockInput block, int planetIndex)
    {
        StarSystem system = GenerateSystem(block);
        if (planetIndex < 0 || planetIndex >= system.Planets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(planetIndex),
                $"Il sistema ha {system.Planets.Count} pianeti, indice richiesto {planetIndex}.");
        }

        Planet planet = system.Planets[planetIndex];
        int planetRadiusKm = planet.RadiusMilliEarth * 6_371 / 1_000;
        int maxMoonRadiusKm = Math.Min(planetRadiusKm / 3, 2_700);
        var weights = MoonTypeWeights(planet.Zone);

        var moons = new Moon[planet.MoonCount];
        for (int j = 0; j < planet.MoonCount; j++)
        {
            var rng = Xoshiro128StarStar.FromSeed(Seeds.Moon(system.BlockHash, planetIndex, j));

            // Base orbit (4 + 3j) planet radii + noise < 1.5 radii: always increasing with j.
            long orbit = (long)planetRadiusKm * (4 + 3 * j) + rng.NextBelow((uint)(planetRadiusKm * 3 / 2));
            int radius = rng.NextInt(100, maxMoonRadiusKm + 1);
            MoonType type = IntMath.Pick<MoonType>(rng, weights);

            moons[j] = new Moon(j, NameGenerator.MoonName(planet.Name, j), type, orbit, radius);
        }

        return moons;
    }

    /// <summary>Density 0..1000: average of gas used and transaction count, each capped at its scale.</summary>
    public static int ComputeDensity(int txCount, long gasUsed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(txCount);
        ArgumentOutOfRangeException.ThrowIfNegative(gasUsed);

        long gasPart = Math.Min(gasUsed, DensityGasScale) * 1_000 / DensityGasScale;
        long txPart = (long)Math.Min(txCount, DensityTxScale) * 1_000 / DensityTxScale;
        return (int)((gasPart + txPart) / 2);
    }

    private static Planet GeneratePlanet(string hash, int index, string systemName, long hzInner, long hzOuter, long orbitOffset)
    {
        var rng = Xoshiro128StarStar.FromSeed(Seeds.Planet(hash, index));

        // Titius-Bode for the Sun: 0.4 / 0.7 / 1.0 / 1.6 / 2.8 / 5.2 / 10 / 19.6 ... AU (in milli-AU),
        // with ±15% noise (never breaks the ordering), scaled by the star's habitable zone.
        long baseMilliAu = index == 0 ? 400 : 400 + (300L << (index - 1));
        int noisePerMille = rng.NextInt(850, 1_151);
        long orbit = baseMilliAu * noisePerMille * hzInner / StarTables.SunHabitableInnerMicroAu + orbitOffset;

        OrbitZone zone =
            orbit < hzInner / 2 ? OrbitZone.Scorched :
            orbit < hzInner ? OrbitZone.Hot :
            orbit <= hzOuter ? OrbitZone.Habitable :
            orbit <= hzOuter * 2 ? OrbitZone.Cold :
            OrbitZone.Frozen;

        PlanetType type = IntMath.Pick<PlanetType>(rng, PlanetTypeWeights(zone));

        int radius = type switch
        {
            PlanetType.Rocky => rng.NextInt(300, 1_801),
            PlanetType.Lava => rng.NextInt(300, 1_601),
            PlanetType.Ice => rng.NextInt(400, 4_501),
            PlanetType.Gas => rng.NextInt(3_500, 13_001),
            _ => throw new InvalidOperationException(),
        };

        int moonCount = type switch
        {
            PlanetType.Gas => rng.NextInt(1, 17),
            PlanetType.Ice => rng.NextInt(0, 7),
            PlanetType.Rocky => rng.NextInt(0, 3),
            PlanetType.Lava => rng.NextInt(0, 2),
            _ => throw new InvalidOperationException(),
        };

        return new Planet(index, NameGenerator.PlanetName(systemName, index), type, zone, orbit, radius, moonCount);
    }

    private static (PlanetType, uint)[] PlanetTypeWeights(OrbitZone zone) => zone switch
    {
        OrbitZone.Scorched => [(PlanetType.Lava, 55), (PlanetType.Rocky, 40), (PlanetType.Gas, 5), (PlanetType.Ice, 0)],
        OrbitZone.Hot => [(PlanetType.Lava, 20), (PlanetType.Rocky, 65), (PlanetType.Gas, 15), (PlanetType.Ice, 0)],
        OrbitZone.Habitable => [(PlanetType.Lava, 0), (PlanetType.Rocky, 75), (PlanetType.Gas, 20), (PlanetType.Ice, 5)],
        OrbitZone.Cold => [(PlanetType.Lava, 0), (PlanetType.Rocky, 35), (PlanetType.Gas, 45), (PlanetType.Ice, 20)],
        OrbitZone.Frozen => [(PlanetType.Lava, 0), (PlanetType.Rocky, 5), (PlanetType.Gas, 50), (PlanetType.Ice, 45)],
        _ => throw new InvalidOperationException(),
    };

    private static (MoonType, uint)[] MoonTypeWeights(OrbitZone zone) => zone switch
    {
        OrbitZone.Scorched or OrbitZone.Hot => [(MoonType.Rocky, 70), (MoonType.Icy, 0), (MoonType.Volcanic, 30)],
        OrbitZone.Habitable => [(MoonType.Rocky, 80), (MoonType.Icy, 15), (MoonType.Volcanic, 5)],
        OrbitZone.Cold => [(MoonType.Rocky, 30), (MoonType.Icy, 60), (MoonType.Volcanic, 10)],
        OrbitZone.Frozen => [(MoonType.Rocky, 10), (MoonType.Icy, 85), (MoonType.Volcanic, 5)],
        _ => throw new InvalidOperationException(),
    };
}
