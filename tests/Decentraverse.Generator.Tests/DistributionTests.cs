namespace Decentraverse.Generator.Tests;

/// <summary>
/// 100.000 sistemi da semi sintetici: le frequenze devono stare entro 5σ dalle probabilità
/// dichiarate. Con 5σ un generatore corretto fallisce circa 1 volta su 1,7 milioni, quindi
/// un fallimento indica un errore nelle tabelle, non sfortuna (e il test è comunque deterministico).
/// </summary>
public class DistributionTests
{
    private const int N = 100_000;

    private static readonly Lazy<StarSystem[]> Generated = new(() =>
        Enumerable.Range(0, N).Select(i => SystemGenerator.GenerateSystem(TestHelpers.SyntheticBlock(i))).ToArray());

    [Fact]
    public void SpectralClassFrequencies()
    {
        // Ogni stella (anche la compagna di una binaria) è estratta in modo indipendente dalla stessa tabella,
        // quindi si contano tutte le stelle, non solo le primarie (lo scambio A/B per massa sposterebbe le primarie).
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
        // Atteso ~1,5 su 100.000: qui si controlla solo che non siano frequenti (la frequenza vera
        // è testata su 2 milioni di semi in RareEventsTests).
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
