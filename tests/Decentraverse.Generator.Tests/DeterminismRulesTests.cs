using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Decentraverse.Generator.Tests;

/// <summary>
/// Guards the non-negotiable rules by scanning the generator's source code: no floating point,
/// no transcendental functions, no randomness, no clock, no I/O. Only the Rendering folder,
/// which decides where things are drawn and never what exists, is exempt.
/// </summary>
public partial class DeterminismRulesTests
{
    [GeneratedRegex(@"\b(double|float|decimal|Half|MathF|Random|DateTime|DateTimeOffset|Stopwatch|Guid|Environment|File|Directory|HttpClient)\b|Math\.(Sin|Cos|Tan|Asin|Acos|Atan|Atan2|Sinh|Cosh|Tanh|Exp|Log|Log2|Log10|Pow|Sqrt|Cbrt|Round|Floor|Ceiling)\b")]
    private static partial Regex Forbidden();

    [Fact]
    public void GeneratorCoreUsesNoFloatingPointRandomnessOrIo()
    {
        string generatorDir = Path.GetFullPath(Path.Combine(TestDirectory(), "..", "..", "src", "Decentraverse.Generator"));
        var files = Directory.GetFiles(generatorDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Rendering{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        Assert.NotEmpty(files);

        var violations = new List<string>();
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].Split("//")[0]; // ignore comments
                if (Forbidden().Match(code) is { Success: true } m)
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}: '{m.Value}'");
                }
            }
        }

        Assert.True(violations.Count == 0, "Forbidden constructs in the generator core:\n" + string.Join("\n", violations));
    }

    private static string TestDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
