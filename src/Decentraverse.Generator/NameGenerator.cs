using System.Text;

namespace Decentraverse.Generator;

/// <summary>
/// Nomi pronunciabili fatti di sillabe (attacco + vocale + coda).
/// Regole: dopo una coda (consonante finale) la sillaba successiva inizia per vocale;
/// dopo una sillaba aperta la successiva inizia per consonante. Così non si formano
/// grappoli impronunciabili né tre vocali di fila.
/// </summary>
internal static class NameGenerator
{
    private static readonly string[] Onsets =
        ["b", "d", "f", "g", "k", "l", "m", "n", "p", "r", "s", "t", "v", "z", "th", "sh", "kr", "tr", "st", "br", "dr", "ph"];

    private static readonly string[] Vowels = ["a", "e", "i", "o", "u", "a", "e", "o", "ae", "ia", "io", "ou"];

    // Le stringhe vuote ripetute rendono le sillabe aperte più frequenti.
    private static readonly string[] Codas = ["", "", "", "", "", "n", "r", "s", "l", "x"];

    private static readonly (int Value, uint Weight)[] SyllableCounts = [(2, 50), (3, 40), (4, 10)];

    /// <summary>Ordine delle estrazioni: numero di sillabe, poi per ogni sillaba [attacco?], vocale, coda.</summary>
    public static string Generate(Xoshiro128StarStar rng)
    {
        int syllables = IntMath.Pick<int>(rng, SyllableCounts);
        var sb = new StringBuilder();
        bool previousHadCoda = false;

        for (int s = 0; s < syllables; s++)
        {
            if (s == 0)
            {
                // Prima sillaba: attacco facoltativo (1 su 4 inizia per vocale).
                if (!rng.NextChance(1, 4))
                {
                    sb.Append(Onsets[rng.NextBelow((uint)Onsets.Length)]);
                }
            }
            else if (!previousHadCoda)
            {
                sb.Append(Onsets[rng.NextBelow((uint)Onsets.Length)]);
            }

            sb.Append(Vowels[rng.NextBelow((uint)Vowels.Length)]);

            // L'ultima sillaba non ha limiti sulla coda; le altre la usano per collegarsi alla successiva.
            string coda = Codas[rng.NextBelow((uint)Codas.Length)];
            sb.Append(coda);
            previousHadCoda = coda.Length > 0;
        }

        sb[0] = char.ToUpperInvariant(sb[0]);
        return sb.ToString();
    }

    private static readonly string[] Roman =
        ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI"];

    /// <summary>Pianeti: lettere minuscole da "b", come nella convenzione degli esopianeti.</summary>
    public static string PlanetName(string systemName, int index) => $"{systemName} {(char)('b' + index)}";

    /// <summary>Lune: numeri romani, come per le lune di Giove.</summary>
    public static string MoonName(string planetName, int index) => $"{planetName} {Roman[index]}";
}
