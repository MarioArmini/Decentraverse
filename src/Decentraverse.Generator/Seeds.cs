using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Decentraverse.Generator;

/// <summary>
/// Semi gerarchici: SHA-256 (UTF-8) di una stringa che contiene versione, hash e percorso.
/// Ogni parte del sistema ha il proprio seme, così si genera solo ciò che serve.
/// L'hash deve essere già in forma canonica (vedi <see cref="BlockHash.Normalize"/>).
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
