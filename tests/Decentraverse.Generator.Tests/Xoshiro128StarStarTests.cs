using Decentraverse.Generator;

namespace Decentraverse.Generator.Tests;

public class Xoshiro128StarStarTests
{
    // Valori ottenuti compilando l'implementazione di riferimento in C
    // (https://prng.di.unimi.it/xoshiro128starstar.c) con lo stesso stato iniziale.
    [Theory]
    [InlineData(1u, 2u, 3u, 4u,
        new uint[] { 11520, 0, 5927040, 70819200, 2031721883, 1637235492, 1287239034, 3734860849, 3729100597, 4258142804 },
        3170714187u)]
    [InlineData(0xDEADBEEFu, 0x01234567u, 0x89ABCDEFu, 0xFEDCBA98u,
        new uint[] { 2576977298, 1162350735, 558593417, 7144843, 2523195487, 3129381430, 3688772568, 1727139565, 2138660864, 1116707375 },
        837872185u)]
    public void MatchesReferenceImplementation(uint s0, uint s1, uint s2, uint s3, uint[] firstTen, uint thousandth)
    {
        var rng = new Xoshiro128StarStar(s0, s1, s2, s3);

        var actual = new uint[10];
        for (int i = 0; i < 10; i++)
        {
            actual[i] = rng.NextUInt32();
        }
        Assert.Equal(firstTen, actual);

        for (int i = 10; i < 999; i++)
        {
            rng.NextUInt32();
        }
        Assert.Equal(thousandth, rng.NextUInt32());
    }

    [Fact]
    public void FromSeedReadsLittleEndianWords()
    {
        byte[] seed =
        [
            0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0xFF, 0xFF, // byte oltre il sedicesimo: ignorati
        ];

        var fromSeed = Xoshiro128StarStar.FromSeed(seed);
        var direct = new Xoshiro128StarStar(1, 2, 3, 4);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(direct.NextUInt32(), fromSeed.NextUInt32());
        }
    }

    [Fact]
    public void RejectsAllZeroStateAndShortSeed()
    {
        Assert.Throws<ArgumentException>(() => new Xoshiro128StarStar(0, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => Xoshiro128StarStar.FromSeed(new byte[16]));
        Assert.Throws<ArgumentException>(() => Xoshiro128StarStar.FromSeed(new byte[15]));
    }

    [Fact]
    public void SameStateGivesSameSequence()
    {
        var a = new Xoshiro128StarStar(42, 7, 1234, 99);
        var b = new Xoshiro128StarStar(42, 7, 1234, 99);

        for (int i = 0; i < 10_000; i++)
        {
            Assert.Equal(a.NextBelow(1000), b.NextBelow(1000));
        }
    }

    [Fact]
    public void NextBelowStaysInRange()
    {
        var rng = new Xoshiro128StarStar(1, 2, 3, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextBelow(0));
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(0u, rng.NextBelow(1));
        }
        for (int i = 0; i < 100_000; i++)
        {
            Assert.InRange(rng.NextBelow(7), 0u, 6u);
            Assert.InRange(rng.NextBelow(uint.MaxValue), 0u, uint.MaxValue - 1);
        }
    }

    [Fact]
    public void NextBelowIsRoughlyUniform()
    {
        // 6 valori, 600.000 estrazioni: attesi 100.000 ciascuno, σ ≈ 289.
        // Tolleranza ±1.500 (≈ 5σ): un fallimento indica un bug, non sfortuna.
        var rng = new Xoshiro128StarStar(0xDEADBEEF, 0x01234567, 0x89ABCDEF, 0xFEDCBA98);
        var counts = new int[6];
        for (int i = 0; i < 600_000; i++)
        {
            counts[rng.NextBelow(6)]++;
        }

        foreach (int c in counts)
        {
            Assert.InRange(c, 98_500, 101_500);
        }
    }

    [Fact]
    public void NextIntCoversWholeRangeIncludingNegatives()
    {
        var rng = new Xoshiro128StarStar(5, 6, 7, 8);
        var seen = new HashSet<int>();
        for (int i = 0; i < 10_000; i++)
        {
            int v = rng.NextInt(-3, 4);
            Assert.InRange(v, -3, 3);
            seen.Add(v);
        }
        Assert.Equal(7, seen.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 5));
    }

    [Fact]
    public void NextChanceMatchesProbability()
    {
        // p = 1/4 su 400.000 prove: attesi 100.000, σ ≈ 274; tolleranza ±1.500.
        var rng = new Xoshiro128StarStar(9, 10, 11, 12);
        int hits = 0;
        for (int i = 0; i < 400_000; i++)
        {
            if (rng.NextChance(1, 4))
            {
                hits++;
            }
        }
        Assert.InRange(hits, 98_500, 101_500);

        Assert.False(rng.NextChance(0, 10));
        Assert.True(rng.NextChance(10, 10));
    }
}
