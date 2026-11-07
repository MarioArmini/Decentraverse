namespace Decentraverse.Generator.Tests;

public class SeedsTests
{
    private const string Hash = "0xabababababababababababababababababababababababababababababababab";

    // Valori calcolati in modo indipendente con: printf '%s' "v1:<hash>:star" | shasum -a 256
    [Fact]
    public void SeedsMatchShasum()
    {
        Assert.Equal("d3e4e136a6e7b5bd471b245e9c9d612e9878c73482d8da99a2f0a57a60ed47fa", Convert.ToHexStringLower(Seeds.Star(Hash)));
        Assert.Equal("9589f6f3dc97ada47adba486d976d211d28b4643795d6d401034252ad7413727", Convert.ToHexStringLower(Seeds.Rare(Hash)));
        Assert.Equal("ab7eae0eebaec8b1be82b582fb5f14462642b5349a6b0ff9d80cc7bc5d006cf5", Convert.ToHexStringLower(Seeds.Planet(Hash, 3)));
        Assert.Equal("cb62fc2d8dfc7db5784d9258cd097baaf4800f30daccadfe0a9d4db5a6875186", Convert.ToHexStringLower(Seeds.Moon(Hash, 3, 0)));
    }

    [Fact]
    public void NormalizeLowercasesAndKeepsPrefix()
    {
        Assert.Equal(Hash, BlockHash.Normalize("0X" + Hash[2..].ToUpperInvariant()));
        Assert.Equal(Hash, BlockHash.Normalize(Hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abababababababababababababababababababababababababababababababab")]
    [InlineData("0xabab")]
    [InlineData("0xgbababababababababababababababababababababababababababababababab")]
    [InlineData("0xabababababababababababababababababababababababababababababababab00")]
    public void NormalizeRejectsMalformedHashes(string hash)
    {
        Assert.Throws<ArgumentException>(() => BlockHash.Normalize(hash));
    }

    [Fact]
    public void UppercaseHashGivesSameSystem()
    {
        var lower = SystemGenerator.GenerateSystem(new BlockInput(1, Hash, 0, 0, 0));
        var upper = SystemGenerator.GenerateSystem(new BlockInput(1, "0x" + Hash[2..].ToUpperInvariant(), 0, 0, 0));
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(lower, TestHelpers.Json),
            System.Text.Json.JsonSerializer.Serialize(upper, TestHelpers.Json));
    }
}
