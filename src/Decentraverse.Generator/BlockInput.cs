namespace Decentraverse.Generator;

/// <summary>I soli dati del blocco che il generatore usa.</summary>
public sealed record BlockInput(long Number, string Hash, long Timestamp, int TxCount, long GasUsed);

public static class BlockHash
{
    /// <summary>
    /// Forma canonica dell'hash usata nei semi: "0x" + 64 cifre esadecimali minuscole.
    /// Chi verifica a mano deve usare esattamente questa forma.
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
