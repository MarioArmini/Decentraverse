using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Decentraverse.Generator.Tests;

internal static class TestHelpers
{
    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Hash sintetico ma ben formato, derivato da un intero: SHA-256("synthetic:i").</summary>
    public static string SyntheticHash(int i) =>
        "0x" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"synthetic:{i}")));

    public static BlockInput SyntheticBlock(int i) => new(i, SyntheticHash(i), 1_700_000_000 + 12L * i, 150, 15_000_000);

    /// <summary>Verifica che un conteggio binomiale stia entro k deviazioni standard dall'atteso.</summary>
    public static void AssertBinomial(long observed, long trials, double p, double k, string label)
    {
        double expected = trials * p;
        double sigma = Math.Sqrt(trials * p * (1 - p));
        double lo = expected - k * sigma;
        double hi = expected + k * sigma;
        Assert.True(observed >= lo && observed <= hi,
            $"{label}: osservati {observed}, attesi {expected:F1} (intervallo {lo:F1}..{hi:F1}, {k}σ).");
    }
}
