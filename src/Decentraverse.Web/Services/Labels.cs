using Decentraverse.Generator;

namespace Decentraverse.Web.Services;

/// <summary>Language-independent labels and colors for generated data. User-facing text lives in <see cref="Texts"/>.</summary>
public static class Labels
{
    public static string Class(Star star) => star.Kind == StarKind.BlackHole ? "BH" : star.Class!.Value.ToString();

    public static uint PlanetColor(PlanetType t) => t switch
    {
        PlanetType.Rocky => 0xB89272,
        PlanetType.Gas => 0xE2BE84,
        PlanetType.Ice => 0x9AD4EC,
        _ => 0xFF6A3A,
    };

    public static uint MoonColor(MoonType t) => t switch
    {
        MoonType.Rocky => 0xA8A29A,
        MoonType.Icy => 0xDDF1FA,
        _ => 0xF2A65A,
    };
}
