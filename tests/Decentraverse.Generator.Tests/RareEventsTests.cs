namespace Decentraverse.Generator.Tests;

public class RareEventsTests
{
    [Fact]
    public void BlackHoleNeedsFirstSixteenBitsZero()
    {
        var seed = new byte[32];
        seed[0] = 0x00; seed[1] = 0x00; seed[2] = 0xFF;
        Assert.True(RareEvents.IsBlackHole(seed));

        seed[1] = 0x01;
        Assert.False(RareEvents.IsBlackHole(seed));

        seed[0] = 0x80; seed[1] = 0x00;
        Assert.False(RareEvents.IsBlackHole(seed));
    }

    [Fact]
    public void BinaryNeedsLowFiveBitsOfThirdByteZero()
    {
        var seed = new byte[32];
        seed[0] = 0xFF;
        seed[2] = 0b1110_0000;
        Assert.True(RareEvents.IsBinary(seed));

        seed[2] = 0b1110_0001;
        Assert.False(RareEvents.IsBinary(seed));
    }

    [Fact]
    public void BlackHoleFrequencyOverTwoMillionSeeds()
    {
        // Atteso 2.000.000 / 65.536 ≈ 30,5. Si testa il solo predicato sul seme "rare"
        // (costa un SHA-256), perché su 100.000 sistemi completi se ne vedrebbe ~1,5.
        const int n = 2_000_000;
        int blackHoles = 0;
        for (int i = 0; i < n; i++)
        {
            if (RareEvents.IsBlackHole(Seeds.Rare(TestHelpers.SyntheticHash(i))))
            {
                blackHoles++;
            }
        }

        TestHelpers.AssertBinomial(blackHoles, n, 1.0 / 65_536, 4, "buchi neri");
    }

    [Fact]
    public void BlackHoleSystemIsGeneratedWhenSeedMatches()
    {
        // Cerca un hash sintetico che produce un buco nero e controlla il sistema risultante.
        for (int i = 0; ; i++)
        {
            string hash = TestHelpers.SyntheticHash(i);
            if (!RareEvents.IsBlackHole(Seeds.Rare(hash)))
            {
                continue;
            }

            var system = SystemGenerator.GenerateSystem(new BlockInput(i, hash, 0, 0, 0));
            Assert.True(system.IsBlackHole);
            Assert.False(system.IsBinary);
            Assert.Equal(StarKind.BlackHole, Assert.Single(system.Stars).Kind);
            Assert.Null(system.Stars[0].Class);
            Assert.Empty(system.Planets);
            return;
        }
    }
}
