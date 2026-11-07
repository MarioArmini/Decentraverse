using System.Buffers.Binary;
using System.Numerics;

namespace Decentraverse.Generator;

/// <summary>
/// PRNG xoshiro128** 1.1 (Blackman &amp; Vigna, https://prng.di.unimi.it/).
/// Uses only unsigned 32-bit integer arithmetic: the output is identical
/// on any platform and in any language that implements the same algorithm.
/// </summary>
public sealed class Xoshiro128StarStar
{
    private uint _s0, _s1, _s2, _s3;

    public Xoshiro128StarStar(uint s0, uint s1, uint s2, uint s3)
    {
        if ((s0 | s1 | s2 | s3) == 0)
        {
            throw new ArgumentException("Lo stato di xoshiro128** non può essere tutto a zero.");
        }

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    /// <summary>
    /// Initializes the state from the first 16 bytes of the seed, read as
    /// four little-endian uint32 values (bytes 0–3 → s0, 4–7 → s1, ...).
    /// </summary>
    public static Xoshiro128StarStar FromSeed(ReadOnlySpan<byte> seed)
    {
        if (seed.Length < 16)
        {
            throw new ArgumentException("Il seme deve avere almeno 16 byte (128 bit).", nameof(seed));
        }

        return new Xoshiro128StarStar(
            BinaryPrimitives.ReadUInt32LittleEndian(seed[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(seed[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(seed[8..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(seed[12..16]));
    }

    /// <summary>Next 32-bit value (direct translation of next() from the C reference).</summary>
    public uint NextUInt32()
    {
        unchecked
        {
            uint result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
            uint t = _s1 << 9;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;

            _s2 ^= t;
            _s3 = BitOperations.RotateLeft(_s3, 11);

            return result;
        }
    }

    /// <summary>
    /// Uniform integer in [0, bound) without bias: rejects values below the threshold
    /// (2^32 mod bound), then takes the modulo. Integer operations only.
    /// </summary>
    public uint NextBelow(uint bound)
    {
        if (bound == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bound), "bound deve essere > 0.");
        }

        uint threshold = unchecked(0u - bound) % bound;
        while (true)
        {
            uint r = NextUInt32();
            if (r >= threshold)
            {
                return r % bound;
            }
        }
    }

    /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive deve essere > minInclusive.");
        }

        uint range = unchecked((uint)(maxExclusive - minInclusive));
        return unchecked(minInclusive + (int)NextBelow(range));
    }

    /// <summary>True with exact probability numerator/denominator.</summary>
    public bool NextChance(uint numerator, uint denominator) => NextBelow(denominator) < numerator;
}
