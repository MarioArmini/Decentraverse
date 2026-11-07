using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Decentraverse.Generator;

/// <summary>
/// Hierarchical seeds: SHA-256 (UTF-8) of a string holding version, hash and path.
/// Each part of the system has its own seed, so only what is needed gets generated.
/// The hash must already be canonical (see <see cref="BlockHash.Normalize"/>).
/// </summary>
public static class Seeds
{
    public static byte[] Star(string hash) => Sha256($"{GeneratorInfo.Version}:{hash}:star");

    public static byte[] Rare(string hash) => Sha256($"{GeneratorInfo.Version}:{hash}:rare");

    public static byte[] Planet(string hash, int planetIndex) =>
        Sha256($"{GeneratorInfo.Version}:{hash}:planet:{Int(planetIndex)}");

    public static byte[] Moon(string hash, int planetIndex, int moonIndex) =>
        Sha256($"{GeneratorInfo.Version}:{hash}:planet:{Int(planetIndex)}:moon:{Int(moonIndex)}");

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static byte[] Sha256(string input) => SHA256.HashData(Encoding.UTF8.GetBytes(input));
}
