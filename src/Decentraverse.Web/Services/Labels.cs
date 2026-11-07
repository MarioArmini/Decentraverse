using System.Globalization;
using Decentraverse.Generator;

namespace Decentraverse.Web.Services;

/// <summary>User-facing texts (Italian) and colors for generated data.</summary>
public static class Labels
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    public static string Class(Star star) => star.Kind == StarKind.BlackHole ? "BH" : star.Class!.Value.ToString();

    public static string StarDescription(Star star) => star.Kind == StarKind.BlackHole
        ? "Buco nero"
        : star.Class switch
        {
            SpectralClass.O => "Gigante blu (O)",
            SpectralClass.B => "Stella blu-bianca (B)",
            SpectralClass.A => "Stella bianca (A)",
            SpectralClass.F => "Stella bianco-gialla (F)",
            SpectralClass.G => "Nana gialla (G)",
            SpectralClass.K => "Nana arancione (K)",
            _ => "Nana rossa (M)",
        };

    public static string PlanetType(PlanetType t) => t switch
    {
        Generator.PlanetType.Rocky => "Roccioso",
        Generator.PlanetType.Gas => "Gassoso",
        Generator.PlanetType.Ice => "Ghiacciato",
        _ => "Lavico",
    };

    public static string Zone(OrbitZone z) => z switch
    {
        OrbitZone.Scorched => "rovente",
        OrbitZone.Hot => "caldo",
        OrbitZone.Habitable => "zona abitabile",
        OrbitZone.Cold => "freddo",
        _ => "gelido",
    };

    public static string MoonType(MoonType t) => t switch
    {
        Generator.MoonType.Rocky => "rocciosa",
        Generator.MoonType.Icy => "ghiacciata",
        _ => "vulcanica",
    };

    public static uint PlanetColor(PlanetType t) => t switch
    {
        Generator.PlanetType.Rocky => 0xB89272,
        Generator.PlanetType.Gas => 0xE2BE84,
        Generator.PlanetType.Ice => 0x9AD4EC,
        _ => 0xFF6A3A,
    };

    public static uint MoonColor(MoonType t) => t switch
    {
        Generator.MoonType.Rocky => 0xA8A29A,
        Generator.MoonType.Icy => 0xDDF1FA,
        _ => 0xF2A65A,
    };

    public static string Au(long microAu) => (microAu / 1_000_000.0).ToString(microAu < 100_000 ? "0.000" : "0.00", It) + " UA";

    public static string Solar(long micro) => (micro / 1_000_000.0).ToString(micro < 10_000 ? "0.0000" : micro < 1_000_000 ? "0.000" : "#,0.##", It);

    public static string Thousandths(int milli) => (milli / 1000.0).ToString("0.00", It);

    public static string Number(long n) => n.ToString("#,0", It);

    public static string Date(long unixSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString("d MMM yyyy, HH:mm:ss", It);
}
