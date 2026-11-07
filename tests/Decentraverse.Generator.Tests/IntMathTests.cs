namespace Decentraverse.Generator.Tests;

public class IntMathTests
{
    [Theory]
    [InlineData(0UL, 0UL)]
    [InlineData(1UL, 1UL)]
    [InlineData(3UL, 1UL)]
    [InlineData(4UL, 2UL)]
    [InlineData(99UL, 9UL)]
    [InlineData(100UL, 10UL)]
    [InlineData(909_090_909_090UL, 953_462UL)]
    [InlineData(ulong.MaxValue, 4_294_967_295UL)]
    public void ISqrtIsFloorOfSquareRoot(ulong n, ulong expected)
    {
        Assert.Equal(expected, IntMath.ISqrt(n));
    }

    [Fact]
    public void ISqrtIsExactAroundPerfectSquares()
    {
        for (ulong r = 1; r < 5_000; r++)
        {
            Assert.Equal(r, IntMath.ISqrt(r * r));
            Assert.Equal(r - 1, IntMath.ISqrt(r * r - 1));
            Assert.Equal(r, IntMath.ISqrt(r * r + 2 * r));
        }

        ulong big = 4_000_000_000UL;
        Assert.Equal(big, IntMath.ISqrt(big * big));
        Assert.Equal(big - 1, IntMath.ISqrt(big * big - 1));
    }

    [Fact]
    public void SunHabitableZoneIsRealistic()
    {
        // Sole: HZ ≈ 0,95–1,37 UA.
        Assert.Equal(953_462, StarTables.SunHabitableInnerMicroAu);
        Assert.InRange(StarTables.HabitableOuterMicroAu(1_000_000), 1_370_000, 1_380_000);
    }
}
