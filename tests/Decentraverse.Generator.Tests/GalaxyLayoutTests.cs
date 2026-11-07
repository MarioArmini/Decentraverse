using Decentraverse.Generator.Rendering;

namespace Decentraverse.Generator.Tests;

public class GalaxyLayoutTests
{
    private const long Reference = 26_362_199;

    private static double Radius((double X, double Y) p) => Math.Sqrt(p.X * p.X + p.Y * p.Y);

    [Fact]
    public void PositionIsDeterministicAndFinite()
    {
        for (int i = 0; i < 1_000; i++)
        {
            long number = Reference - i;
            string hash = TestHelpers.SyntheticHash(i);
            var a = GalaxyLayout.Position(number, hash, Reference);
            var b = GalaxyLayout.Position(number, hash, Reference);

            Assert.Equal(a, b);
            Assert.True(double.IsFinite(a.X) && double.IsFinite(a.Y));
        }
    }

    [Fact]
    public void OlderBlocksSitCloserToTheCentre()
    {
        // Averaged over 200 blocks to smooth out the jitter.
        double Mean(long from) => Enumerable.Range(0, 200)
            .Average(i => Radius(GalaxyLayout.Position(from - i, TestHelpers.SyntheticHash(i), Reference)));

        double newest = Mean(Reference);
        double older = Mean(Reference - 400);
        double oldest = Mean(Reference - 800);

        Assert.True(newest > older && older > oldest, $"{newest:F0} > {older:F0} > {oldest:F0}");
        Assert.InRange(newest, 0.5 * GalaxyLayout.ReferenceRadius, 1.5 * GalaxyLayout.ReferenceRadius);
    }

    [Fact]
    public void ChangingTheReferenceOnlyScalesThePicture()
    {
        string h1 = TestHelpers.SyntheticHash(1), h2 = TestHelpers.SyntheticHash(2);
        var a1 = GalaxyLayout.Position(Reference - 10, h1, Reference);
        var b1 = GalaxyLayout.Position(Reference - 300, h2, Reference);
        var a2 = GalaxyLayout.Position(Reference - 10, h1, Reference + 32);
        var b2 = GalaxyLayout.Position(Reference - 300, h2, Reference + 32);

        double scaleA = Radius(a2) / Radius(a1);
        double scaleB = Radius(b2) / Radius(b1);
        Assert.Equal(scaleA, scaleB, 9);
        Assert.True(scaleA < 1);
        Assert.Equal(Math.Atan2(a1.Y, a1.X), Math.Atan2(a2.Y, a2.X), 9);
    }
}
