using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Decentraverse.Generator.Tests;

/// <summary>
/// Golden test: the output for 5 real blocks (systems + all moons) is compared with the file
/// Golden/&lt;version&gt;.json committed to the repo.
///
/// - Output changed with the same version → the test fails. That is the point: the universe must not
///   change silently. If the change is intended, bump GeneratorInfo.Version.
/// - New version without a file → the test fails and explains how to create it:
///       DECENTRAVERSE_WRITE_GOLDEN=1 dotnet test
///   The file of an existing version is NEVER overwritten.
/// </summary>
public class GoldenTests
{
    private sealed record GoldenEntry(BlockInput Block, StarSystem System, IReadOnlyList<IReadOnlyList<Moon>> Moons);

    [Fact]
    public void OutputMatchesGoldenFileForCurrentVersion()
    {
        string dir = SourceDirectory();
        string goldenPath = Path.Combine(dir, "Golden", $"{GeneratorInfo.Version}.json");
        string actual = JsonSerializer.Serialize(GenerateAll(dir), TestHelpers.Json) + "\n";

        if (!File.Exists(goldenPath))
        {
            if (Environment.GetEnvironmentVariable("DECENTRAVERSE_WRITE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);
                File.WriteAllText(goldenPath, actual);
                return;
            }

            Assert.Fail($"Manca {goldenPath} per la versione {GeneratorInfo.Version}. " +
                        "Se hai appena alzato la versione, crealo con: DECENTRAVERSE_WRITE_GOLDEN=1 dotnet test");
        }

        string expected = File.ReadAllText(goldenPath).ReplaceLineEndings("\n");
        if (expected != actual)
        {
            Assert.Fail($"L'output del generatore {GeneratorInfo.Version} è cambiato rispetto a {goldenPath}. " +
                        "Se il cambiamento è voluto, alza GeneratorInfo.Version e genera il nuovo file golden; " +
                        $"altrimenti è una regressione.\nPrima differenza: {FirstDifference(expected, actual)}");
        }
    }

    [Fact]
    public void GoldenBlocksAreRealAndWellFormed()
    {
        var blocks = LoadBlocks(SourceDirectory());
        Assert.Equal(5, blocks.Count);
        Assert.All(blocks, b => Assert.Equal(b.Hash, BlockHash.Normalize(b.Hash)));
        Assert.Equal(blocks.Count, blocks.Select(b => b.Hash).Distinct().Count());
    }

    private static List<GoldenEntry> GenerateAll(string dir) =>
        LoadBlocks(dir).Select(block =>
        {
            var system = SystemGenerator.GenerateSystem(block);
            var moons = system.Planets.Select(p => SystemGenerator.GenerateMoons(block, p.Index)).ToList();
            return new GoldenEntry(block, system, moons);
        }).ToList();

    private static List<BlockInput> LoadBlocks(string dir) =>
        JsonSerializer.Deserialize<List<BlockInput>>(
            File.ReadAllText(Path.Combine(dir, "Fixtures", "golden-blocks.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static string FirstDifference(string expected, string actual)
    {
        var e = expected.Split('\n');
        var a = actual.Split('\n');
        for (int i = 0; i < Math.Max(e.Length, a.Length); i++)
        {
            string el = i < e.Length ? e[i] : "<fine>";
            string al = i < a.Length ? a[i] : "<fine>";
            if (el != al)
            {
                return $"riga {i + 1}: atteso «{el.Trim()}», ottenuto «{al.Trim()}»";
            }
        }
        return "nessuna";
    }

    /// <summary>The folder of this source file: golden files are read and written there, not in bin/.</summary>
    private static string SourceDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
