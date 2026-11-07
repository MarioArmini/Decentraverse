namespace Decentraverse.Generator.Tests;

public class InvariantTests
{
    private static IEnumerable<StarSystem> Systems(int count) =>
        Enumerable.Range(0, count).Select(i => SystemGenerator.GenerateSystem(TestHelpers.SyntheticBlock(i)));

    [Fact]
    public void PlanetsAreOrderedAndConsistentWithTheirZone()
    {
        foreach (var s in Systems(20_000))
        {
            Assert.InRange(s.Planets.Count, 0, 10);
            Assert.Equal(GeneratorInfo.Version, s.GeneratorVersion);
            Assert.False(string.IsNullOrWhiteSpace(s.Name));
            Assert.True(char.IsUpper(s.Name[0]));

            if (!s.IsBlackHole)
            {
                Assert.True(s.HabitableZoneInnerMicroAu > 0);
                Assert.True(s.HabitableZoneOuterMicroAu > s.HabitableZoneInnerMicroAu);
            }

            long previous = 0;
            foreach (var p in s.Planets)
            {
                Assert.True(p.OrbitMicroAu > previous, $"{s.Name}: orbite non crescenti");
                previous = p.OrbitMicroAu;

                // Niente lava fuori dalle zone calde, niente ghiaccio troppo vicino alla stella.
                if (p.Type == PlanetType.Lava)
                {
                    Assert.Contains(p.Zone, new[] { OrbitZone.Scorched, OrbitZone.Hot });
                }
                if (p.Type == PlanetType.Ice)
                {
                    Assert.DoesNotContain(p.Zone, new[] { OrbitZone.Scorched, OrbitZone.Hot });
                }

                Assert.True(p.OrbitMicroAu > 3 * s.BinarySeparationMicroAu);
                Assert.StartsWith(s.Name + " ", p.Name);
            }
        }
    }

    [Fact]
    public void BinaryStarsAreOrderedByMass()
    {
        int binaries = 0;
        foreach (var s in Systems(20_000).Where(s => s.IsBinary))
        {
            binaries++;
            Assert.Equal(2, s.Stars.Count);
            Assert.Equal("A", s.Stars[0].Designation);
            Assert.Equal("B", s.Stars[1].Designation);
            Assert.True(s.Stars[0].MassMilliSolar >= s.Stars[1].MassMilliSolar);
            Assert.True(s.BinarySeparationMicroAu > 0);
        }

        Assert.True(binaries > 0);
    }

    [Fact]
    public void MoonsAreOrderedAndSmallerThanTheirPlanet()
    {
        for (int i = 0; i < 2_000; i++)
        {
            var block = TestHelpers.SyntheticBlock(i);
            var s = SystemGenerator.GenerateSystem(block);
            foreach (var p in s.Planets)
            {
                var moons = SystemGenerator.GenerateMoons(block, p.Index);
                Assert.Equal(p.MoonCount, moons.Count);

                int planetRadiusKm = p.RadiusMilliEarth * 6_371 / 1_000;
                long previous = planetRadiusKm;
                foreach (var m in moons)
                {
                    Assert.True(m.OrbitKm > previous);
                    previous = m.OrbitKm;
                    Assert.InRange(m.RadiusKm, 100, planetRadiusKm / 3);
                    Assert.StartsWith(p.Name + " ", m.Name);
                }
            }
        }
    }

    [Fact]
    public void GenerateMoonsRejectsMissingPlanet()
    {
        var block = TestHelpers.SyntheticBlock(1);
        int count = SystemGenerator.GenerateSystem(block).Planets.Count;
        Assert.Throws<ArgumentOutOfRangeException>(() => SystemGenerator.GenerateMoons(block, count));
        Assert.Throws<ArgumentOutOfRangeException>(() => SystemGenerator.GenerateMoons(block, -1));
    }

    [Theory]
    [InlineData(0, 0L, 0)]
    [InlineData(500, 60_000_000L, 1_000)]
    [InlineData(5_000, 90_000_000L, 1_000)]
    [InlineData(250, 30_000_000L, 500)]
    [InlineData(0, 60_000_000L, 500)]
    public void DensityIsAverageOfSaturatedGasAndTx(int tx, long gas, int expected)
    {
        Assert.Equal(expected, SystemGenerator.ComputeDensity(tx, gas));
    }

    [Fact]
    public void DensityRejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SystemGenerator.ComputeDensity(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SystemGenerator.ComputeDensity(0, -1));
    }
}
