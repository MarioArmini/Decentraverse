namespace Decentraverse.Generator;

/// <summary>The only block data the generator uses.</summary>
public sealed record BlockInput(long Number, string Hash, long Timestamp, int TxCount, long GasUsed);

public static class BlockHash
{
    /// <summary>
    /// Canonical hash form used in seeds: "0x" + 64 lowercase hex digits.
    /// Anyone verifying by hand must use exactly this form.
    /// </summary>
    public static string Normalize(string hash)
    {
        ArgumentNullException.ThrowIfNull(hash);

        if (hash.Length != 66 || hash[0] != '0' || (hash[1] != 'x' && hash[1] != 'X'))
        {
            throw new ArgumentException($"Hash non valido: atteso 0x + 64 cifre esadecimali, ricevuto '{hash}'.", nameof(hash));
        }

        Span<char> result = stackalloc char[66];
        result[0] = '0';
        result[1] = 'x';
        for (int i = 2; i < 66; i++)
        {
            char c = hash[i];
            result[i] = c switch
            {
                >= '0' and <= '9' or >= 'a' and <= 'f' => c,
                >= 'A' and <= 'F' => (char)(c + ('a' - 'A')),
                _ => throw new ArgumentException($"Carattere non esadecimale '{c}' nell'hash.", nameof(hash)),
            };
        }

        return new string(result);
    }
}
