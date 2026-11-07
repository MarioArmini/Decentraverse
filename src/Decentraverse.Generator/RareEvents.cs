namespace Decentraverse.Generator;

/// <summary>
/// Eventi rari decisi da pattern di bit del seme "rare" (non dal PRNG),
/// così le probabilità sono esatte e verificabili a occhio sull'hash esadecimale.
/// </summary>
public static class RareEvents
{
    /// <summary>Primi 16 bit a zero (il seme in esadecimale inizia con "0000"): 1 su 65.536.</summary>
    public static bool IsBlackHole(ReadOnlySpan<byte> rareSeed) => rareSeed[0] == 0 && rareSeed[1] == 0;

    /// <summary>5 bit bassi del terzo byte a zero: 1 su 32. Ignorato se il sistema è un buco nero.</summary>
    public static bool IsBinary(ReadOnlySpan<byte> rareSeed) => (rareSeed[2] & 0x1F) == 0;
}
