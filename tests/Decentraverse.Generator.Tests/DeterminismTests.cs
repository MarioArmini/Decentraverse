using System.Text.Json;

namespace Decentraverse.Generator.Tests;

public class DeterminismTests
{
    [Fact]
    public void SameBlockGivesIdenticalSystemAndMoons()
    {
        for (int i = 0; i < 500; i++)
        {
            var block = TestHelpers.SyntheticBlock(i);
            var a = SystemGenerator.GenerateSystem(block);
            var b = SystemGenerator.GenerateSystem(block with { });

            // Confronto "deep" tramite JSON: copre tutti i campi, liste comprese.
            Assert.Equal(JsonSerializer.Serialize(a, TestHelpers.Json), JsonSerializer.Serialize(b, TestHelpers.Json));

            for (int p = 0; p < a.Planets.Count; p++)
            {
                Assert.Equal(
                    JsonSerializer.Serialize(SystemGenerator.GenerateMoons(block, p), TestHelpers.Json),
                    JsonSerializer.Serialize(SystemGenerator.GenerateMoons(block, p), TestHelpers.Json));
            }
        }
    }

    [Fact]
    public void DifferentHashesGiveDifferentSystems()
    {
        var names = new HashSet<string>();
        for (int i = 0; i < 1_000; i++)
        {
            var s = SystemGenerator.GenerateSystem(TestHelpers.SyntheticBlock(i));
            names.Add(JsonSerializer.Serialize(s with { BlockNumber = 0, BlockHash = "" }, TestHelpers.Json));
        }

        Assert.Equal(1_000, names.Count);
    }

    [Fact]
    public void OnlyHashDecidesTheSystemNotNumberOrTimestamp()
    {
        var block = TestHelpers.SyntheticBlock(7);
        var a = SystemGenerator.GenerateSystem(block);
        var b = SystemGenerator.GenerateSystem(block with { Number = 999, Timestamp = 1 });

        Assert.Equal(
            JsonSerializer.Serialize(a with { BlockNumber = 0 }, TestHelpers.Json),
            JsonSerializer.Serialize(b with { BlockNumber = 0 }, TestHelpers.Json));
    }
}
