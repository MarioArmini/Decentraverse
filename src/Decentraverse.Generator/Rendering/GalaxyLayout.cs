namespace Decentraverse.Generator.Rendering;

/// <summary>
/// Where each block's star is drawn on the galaxy map. RENDERING ONLY: it decides where a star
/// is shown, never what exists, so floating point and trigonometry are allowed here (and only
/// here: the rest of the generator is guarded by a test that forbids them).
///
/// Model: a multi-arm logarithmic spiral. Block n (counted from <see cref="OriginBlock"/>) goes to
/// arm n mod Arms, at step t = n / Arms along that arm. Each step turns the arm by 2π/StepsPerTurn
/// and multiplies the radius by e^(b·2π/StepsPerTurn): old blocks sit towards the centre and the
/// galaxy grows outwards.
///
/// Because a logarithmic spiral is self-similar, absolute radii would span astronomically large
/// numbers after millions of blocks. Positions are therefore expressed relative to a reference
/// block (normally the newest one), which sits at radius <see cref="ReferenceRadius"/>. Changing
/// the reference only scales the whole picture uniformly (a camera zoom); each star keeps its
/// place in the spiral.
/// </summary>
public static class GalaxyLayout
{
    /// <summary>The Merge: the first proof-of-stake block, from which blocks can be finalized.</summary>
    public const long OriginBlock = 15_537_394;

    public const int Arms = 4;

    /// <summary>Steps along one arm for a full turn: with 4 arms, 800 blocks (≈ 2.7 hours).</summary>
    public const int StepsPerTurn = 200;

    /// <summary>Logarithmic spiral growth rate per radian (pitch angle ≈ 14°).</summary>
    public const double SpiralGrowth = 0.25;

    public const double ReferenceRadius = 1000;

    /// <summary>Scatter around the arm, as fractions of the local radius and of a radian.</summary>
    public const double RadialJitter = 0.10;
    public const double AngularJitter = 0.12;

    public static (double X, double Y) Position(long blockNumber, string blockHash, long referenceBlockNumber)
    {
        var (arm, step) = ArmAndStep(blockNumber);
        var (_, referenceStep) = ArmAndStep(referenceBlockNumber);

        // Jitter comes from bytes 16..19 of the star seed: the PRNG only uses bytes 0..15.
        byte[] seed = Seeds.Star(BlockHash.Normalize(blockHash));
        double jitterRadial = Signed(seed[16], seed[17]);
        double jitterAngular = Signed(seed[18], seed[19]);

        double stepAngle = 2 * Math.PI / StepsPerTurn;
        double logRadius = SpiralGrowth * stepAngle * (step - referenceStep) + RadialJitter * jitterRadial;
        double angle = stepAngle * PositiveModulo(step, StepsPerTurn)
                       + 2 * Math.PI * arm / Arms
                       + AngularJitter * jitterAngular;

        double radius = ReferenceRadius * Math.Exp(logRadius);
        return (radius * Math.Cos(angle), radius * Math.Sin(angle));
    }

    private static (int Arm, long Step) ArmAndStep(long blockNumber)
    {
        long n = blockNumber - OriginBlock;
        long step = n >= 0 ? n / Arms : -((-n + Arms - 1) / Arms);
        return ((int)(n - step * Arms), step);
    }

    private static long PositiveModulo(long value, long modulus) => ((value % modulus) + modulus) % modulus;

    /// <summary>Two bytes → a value in [-1, 1).</summary>
    private static double Signed(byte high, byte low) => ((high << 8) | low) / 32768.0 - 1.0;
}
