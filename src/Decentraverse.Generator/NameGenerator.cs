using System.Text;

namespace Decentraverse.Generator;

/// <summary>
/// Pronounceable names built from syllables (onset + vowel + coda).
/// Rules: after a coda (final consonant) the next syllable starts with a vowel;
/// after an open syllable the next one starts with a consonant. This avoids
/// unpronounceable clusters and three vowels in a row.
/// </summary>
internal static class NameGenerator
{
    private static readonly string[] Onsets =
        ["b", "d", "f", "g", "k", "l", "m", "n", "p", "r", "s", "t", "v", "z", "th", "sh", "kr", "tr", "st", "br", "dr", "ph"];

    private static readonly string[] Vowels = ["a", "e", "i", "o", "u", "a", "e", "o", "ae", "ia", "io", "ou"];

    // Repeated empty strings make open syllables more frequent.
    private static readonly string[] Codas = ["", "", "", "", "", "n", "r", "s", "l", "x"];

    private static readonly (int Value, uint Weight)[] SyllableCounts = [(2, 50), (3, 40), (4, 10)];

    /// <summary>Draw order: number of syllables, then for each syllable [onset?], vowel, coda.</summary>
    public static string Generate(Xoshiro128StarStar rng)
    {
        int syllables = IntMath.Pick<int>(rng, SyllableCounts);
        var sb = new StringBuilder();
        bool previousHadCoda = false;

        for (int s = 0; s < syllables; s++)
        {
            if (s == 0)
            {
                // First syllable: optional onset (1 in 4 starts with a vowel).
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

            // The last syllable has no coda constraint; the others use it to link to the next one.
            string coda = Codas[rng.NextBelow((uint)Codas.Length)];
            sb.Append(coda);
            previousHadCoda = coda.Length > 0;
        }

        sb[0] = char.ToUpperInvariant(sb[0]);
        return sb.ToString();
    }

    private static readonly string[] Roman =
        ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI"];

    /// <summary>Planets: lowercase letters from "b", following the exoplanet convention.</summary>
    public static string PlanetName(string systemName, int index) => $"{systemName} {(char)('b' + index)}";

    /// <summary>Moons: Roman numerals, as for Jupiter's moons.</summary>
    public static string MoonName(string planetName, int index) => $"{planetName} {Roman[index]}";
}
