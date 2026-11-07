namespace Decentraverse.Generator;

/// <summary>
/// Rare events decided by bit patterns of the "rare" seed (not by the PRNG),
/// so the probabilities are exact and can be checked by eye on the hex hash.
/// </summary>
public static class RareEvents
{
    /// <summary>First 16 bits zero (the hex seed starts with "0000"): 1 in 65,536.</summary>
    public static bool IsBlackHole(ReadOnlySpan<byte> rareSeed) => rareSeed[0] == 0 && rareSeed[1] == 0;

    /// <summary>Low 5 bits of the third byte zero: 1 in 32. Ignored if the system is a black hole.</summary>
    public static bool IsBinary(ReadOnlySpan<byte> rareSeed) => (rareSeed[2] & 0x1F) == 0;
}
