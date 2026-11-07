namespace Decentraverse.Generator;

internal static class IntMath
{
    /// <summary>Integer square root (floor), bit-by-bit method: integers only, exact result.</summary>
    public static ulong ISqrt(ulong n)
    {
        ulong result = 0;
        ulong bit = 1UL << 62;
        while (bit > n)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (n >= result + bit)
            {
                n -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }
            bit >>= 2;
        }

        return result;
    }

    /// <summary>Integer linear interpolation: lo + (hi - lo) * perMille / 1000.</summary>
    public static long Lerp(long lo, long hi, int perMille) => lo + (hi - lo) * perMille / 1000;

    public static T Pick<T>(Xoshiro128StarStar rng, ReadOnlySpan<(T Value, uint Weight)> items)
    {
        uint total = 0;
        foreach (var item in items)
        {
            total += item.Weight;
        }

        uint r = rng.NextBelow(total);
        foreach (var item in items)
        {
            if (r < item.Weight)
            {
                return item.Value;
            }
            r -= item.Weight;
        }

        throw new InvalidOperationException("Irraggiungibile: pesi incoerenti.");
    }
}
