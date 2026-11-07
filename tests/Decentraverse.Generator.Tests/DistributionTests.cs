namespace Decentraverse.Generator.Tests;

/// <summary>
/// 100,000 systems from synthetic seeds: frequencies must lie within 5σ of the declared
/// probabilities. At 5σ a correct generator fails about once in 1.7 million runs, so
/// a failure means an error in the tables, not bad luck (and the test is deterministic anyway).
/// </summary>
public class DistributionTests
{
    private const int N = 100_000;

    private static readonly Lazy<StarSystem[]> Generated = new(() =>
        Enumerable.Range(0, N).Select(i => SystemGenerator.GenerateSystem(TestHelpers.SyntheticBlock(i))).ToArray());

    [Fact]
    public void SpectralClassFrequencies()
    {
        // Every star (binary companions included) is drawn independently from the same table,
        // so all stars are counted, not just primaries (the A/B swap by mass would skew primaries).
        var stars = Generated.Value.SelectMany(s => s.Stars).Where(s => s.Kind == StarKind.Star).ToArray();

        foreach (var spec in StarTables.Classes)
        {
            long count = stars.LongCount(s => s.Class == spec.Class);
            TestHelpers.AssertBinomial(count, stars.Length, spec.Weight / (double)StarTables.TotalWeight, 5, $"classe {spec.Class}");
        }
    }

    [Fact]
    public void BinaryFrequency()
    {
        long binaries = Generated.Value.LongCount(s => s.IsBinary);
        TestHelpers.AssertBinomial(binaries, N, (1.0 / 32) * (1 - 1.0 / 65_536), 5, "binarie");
    }

    [Fact]
    public void BlackHolesAreRareInFullSystems()
    {
        // ~1.5 expected in 100,000: here we only check they are not frequent (the real frequency
        // is tested over 2 million seeds in RareEventsTests).
        Assert.InRange(Generated.Value.Count(s => s.IsBlackHole), 0, 12);
    }

    [Fact]
    public void PlanetCountFrequencies()
    {
        uint[] weights = [8, 8, 10, 12, 13, 13, 12, 9, 7, 5, 3];
        var normal = Generated.Value.Where(s => !s.IsBlackHole).ToArray();

        for (int k = 0; k <= 10; k++)
        {
            long count = normal.LongCount(s => s.Planets.Count == k);
            TestHelpers.AssertBinomial(count, normal.Length, weights[k] / 100.0, 5, $"{k} pianeti");
        }
    }

    [Fact]
    public void EveryPlanetTypeAndZoneAppears()
    {
        var planets = Generated.Value.SelectMany(s => s.Planets).ToArray();
        foreach (PlanetType t in Enum.GetValues<PlanetType>())
        {
            Assert.Contains(planets, p => p.Type == t);
        }
        foreach (OrbitZone z in Enum.GetValues<OrbitZone>())
        {
            Assert.Contains(planets, p => p.Zone == z);
        }
    }
}
