using System.Globalization;
using Decentraverse.Generator;

namespace Decentraverse.Web.Services;

public enum Language { En, It }

/// <summary>
/// All user-facing text, in English and Italian, plus number and date formatting for the
/// current language. A plain catalog: no resource files or extra packages needed for two languages.
/// </summary>
public sealed class Texts
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

    public Language Language { get; private set; } = Language.En;

    public CultureInfo Culture => Language == Language.It ? Italian : English;

    /// <summary>Language code used for the html lang attribute and stored preferences.</summary>
    public string Code => Language == Language.It ? "it" : "en";

    public void Set(string? code) => Language = code is not null && code.StartsWith("it", StringComparison.OrdinalIgnoreCase)
        ? Language.It
        : Language.En;

    private string T(string en, string it) => Language == Language.It ? it : en;

    // Galaxy view ---------------------------------------------------------------------------

    public string Tagline => T("Every finalized Ethereum block is a star system.", "Ogni blocco finalizzato di Ethereum è un sistema stellare.");
    public string ContactingIndexer => T("Contacting the indexer…", "Contatto l'indexer…");
    public string Generating(int count) => T($"Generating {Number(count)} systems…", $"Genero {Number(count)} sistemi…");
    public string Live(int systems, long latest) => T(
        $"Live from the indexer · {Number(systems)} systems · latest block #{Number(latest)}",
        $"Live dall'indexer · {Number(systems)} sistemi · ultimo blocco #{Number(latest)}");
    public string Offline(int blocks) => T(
        $"Offline: indexer unreachable, showing {Number(blocks)} saved blocks",
        $"Offline: indexer non raggiungibile, uso {Number(blocks)} blocchi salvati");
    public string ClassName(string cls) => cls == "BH" ? T("Black holes", "Buchi neri") : T($"Class {cls}", $"Classe {cls}");
    public string Hint => T(
        "Drag to move · scroll to zoom · click a star to open its system",
        "Trascina per spostarti · rotella per lo zoom · clic su una stella per aprire il suo sistema");
    public string LanguageSwitch => T("Language", "Lingua");

    /// <summary>Words used by the JavaScript tooltip.</summary>
    public object RendererTexts => new
    {
        block = T("block", "blocco"),
        @class = T("class", "classe"),
        binary = T("binary", "binaria"),
        blackHole = T("black hole", "buco nero"),
        locale = Culture.Name,
    };

    // System panel ------------------------------------------------------------------------------

    public string Back => T("← Back to the galaxy", "← Torna alla galassia");
    public string BinarySystem => T("binary system", "sistema binario");
    public string GeneratedFrom => T("Generated from this hash", "Generato da questo hash");
    public string OnEtherscan(long block) => T($"Block #{Number(block)} on Etherscan ↗", $"Blocco #{Number(block)} su Etherscan ↗");
    public string HowToVerify => T("How to verify it", "Come verificarlo");
    public string VerifyIntro => T(
        "Every part of the system comes from a SHA-256 seed. Anyone can recompute them, for example in a terminal:",
        "Ogni parte del sistema nasce da un seme SHA-256. Chiunque può ricalcolarli, ad esempio da terminale:");
    public string StarSeed => T("star seed", "seme stella");
    public string RareSeed => T("rare-events seed", "seme eventi rari");
    public string VerifyRareRule => T(
        "It is a black hole if the rare-events seed starts with 0000 (1 in 65,536). Generator rules:",
        "Buco nero se il seme degli eventi rari inizia con 0000 (1 su 65.536). Regole del generatore:");
    public string BlackHoleNote => T(
        "A black hole: it happens once every 65,536 blocks. The rare-events seed of this block starts with 0000.",
        "Un buco nero: capita una volta ogni 65.536 blocchi. Il seme degli eventi rari di questo blocco inizia con 0000.");

    public string StarHeading(bool binary) => binary ? T("Stars", "Stelle") : T("Star", "Stella");
    public string StarDesignation(string designation) => T($"Star {designation}", $"Stella {designation}");
    public string ClassLower(SpectralClass? cls) => T($"class {cls}", $"classe {cls}");
    public string Mass => T("Mass", "Massa");
    public string Temperature => T("Temperature", "Temperatura");
    public string Luminosity => T("Luminosity", "Luminosità");
    public string Separation => T("Separation", "Separazione");
    public string HabitableZone => T("Habitable zone", "Zona abitabile");

    public string BlockHeading => T("Block", "Blocco");
    public string Date => T("Date", "Data");
    public string Transactions => T("Transactions", "Transazioni");
    public string GasUsed => T("Gas used", "Gas usato");
    public string Density => T("Density", "Densità");

    public string Planets(int count) => T($"Planets ({count})", $"Pianeti ({count})");
    public string MoonCount(int count) => count == 1 ? T("1 moon", "1 luna") : T($"{count} moons", $"{count} lune");
    public string NoMoons => T("No moons.", "Nessuna luna.");
    public string NoPlanets => T("No planets in this system.", "Nessun pianeta in questo sistema.");
    public string MoonDetails(Moon m) => T(
        $"{MoonType(m.Type)} · radius {Number(m.RadiusKm)} km · orbit {Number(m.OrbitKm)} km",
        $"{MoonType(m.Type)} · raggio {Number(m.RadiusKm)} km · orbita {Number(m.OrbitKm)} km");

    // Generated data ----------------------------------------------------------------------------

    public string StarDescription(Star star) => star.Kind == StarKind.BlackHole
        ? T("Black hole", "Buco nero")
        : star.Class switch
        {
            SpectralClass.O => T("Blue giant (O)", "Gigante blu (O)"),
            SpectralClass.B => T("Blue-white star (B)", "Stella blu-bianca (B)"),
            SpectralClass.A => T("White star (A)", "Stella bianca (A)"),
            SpectralClass.F => T("Yellow-white star (F)", "Stella bianco-gialla (F)"),
            SpectralClass.G => T("Yellow dwarf (G)", "Nana gialla (G)"),
            SpectralClass.K => T("Orange dwarf (K)", "Nana arancione (K)"),
            _ => T("Red dwarf (M)", "Nana rossa (M)"),
        };

    public string PlanetType(PlanetType t) => t switch
    {
        Generator.PlanetType.Rocky => T("Rocky", "Roccioso"),
        Generator.PlanetType.Gas => T("Gas giant", "Gassoso"),
        Generator.PlanetType.Ice => T("Icy", "Ghiacciato"),
        _ => T("Lava", "Lavico"),
    };

    public string Zone(OrbitZone z) => z switch
    {
        OrbitZone.Scorched => T("scorching", "rovente"),
        OrbitZone.Hot => T("hot", "caldo"),
        OrbitZone.Habitable => T("habitable zone", "zona abitabile"),
        OrbitZone.Cold => T("cold", "freddo"),
        _ => T("frozen", "gelido"),
    };

    public string MoonType(MoonType t) => t switch
    {
        Generator.MoonType.Rocky => T("rocky", "rocciosa"),
        Generator.MoonType.Icy => T("icy", "ghiacciata"),
        _ => T("volcanic", "vulcanica"),
    };

    // Formatting ----------------------------------------------------------------------------------

    public string Au(long microAu) =>
        (microAu / 1_000_000.0).ToString(microAu < 100_000 ? "0.000" : "0.00", Culture) + T(" AU", " UA");

    public string Solar(long micro) =>
        (micro / 1_000_000.0).ToString(micro < 10_000 ? "0.0000" : micro < 1_000_000 ? "0.000" : "#,0.##", Culture);

    public string Thousandths(int milli) => (milli / 1000.0).ToString("0.00", Culture);

    public string Number(long n) => n.ToString("#,0", Culture);

    public string DateTime(long unixSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString(T("MMM d, yyyy, HH:mm:ss", "d MMM yyyy, HH:mm:ss"), Culture);
}
