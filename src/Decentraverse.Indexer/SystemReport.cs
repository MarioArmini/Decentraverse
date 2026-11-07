using System.Text.Json;
using System.Text.Json.Serialization;
using Decentraverse.Generator;

namespace Decentraverse.Indexer;

/// <summary>
/// JSON report for the "system" command: the block, the exact seed inputs and their SHA-256,
/// and the generated system. Everything needed to check a system by hand.
/// </summary>
public static class SystemReport
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Build(BlockRecord record, bool includeMoons)
    {
        var block = new BlockInput(record.Number, record.Hash, record.Timestamp, record.TxCount, record.GasUsed);
        var system = SystemGenerator.GenerateSystem(block);
        string hash = system.BlockHash;

        var report = new
        {
            block = record,
            seeds = new
            {
                starInput = $"{GeneratorInfo.Version}:{hash}:star",
                star = Convert.ToHexStringLower(Seeds.Star(hash)),
                rareInput = $"{GeneratorInfo.Version}:{hash}:rare",
                rare = Convert.ToHexStringLower(Seeds.Rare(hash)),
            },
            system,
            moons = includeMoons
                ? system.Planets.Select(p => SystemGenerator.GenerateMoons(block, p.Index)).ToArray()
                : null,
        };

        return JsonSerializer.Serialize(report, Json);
    }
}
