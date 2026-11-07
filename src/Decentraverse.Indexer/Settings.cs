namespace Decentraverse.Indexer;

/// <summary>Configurazione da variabili d'ambiente, con il file .env della radice del repo come fallback.</summary>
public sealed record Settings(Uri RpcUrl, string DatabasePath, int ApiPort)
{
    public static Settings Load()
    {
        string root = FindRepoRoot();
        LoadEnvFile(Path.Combine(root, ".env"));

        string? rpc = Environment.GetEnvironmentVariable("ETH_RPC_URL");
        if (string.IsNullOrWhiteSpace(rpc) || rpc.Contains('<'))
        {
            throw new InvalidOperationException(
                "ETH_RPC_URL non è impostata (o contiene ancora il segnaposto). Copia .env.example in .env e modificalo.");
        }

        string db = Environment.GetEnvironmentVariable("DECENTRAVERSE_DB") ?? "data/blocks.db";
        string port = Environment.GetEnvironmentVariable("DECENTRAVERSE_API_PORT") ?? "8787";

        return new Settings(new Uri(rpc), Path.GetFullPath(db, root), int.Parse(port, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>La cartella che contiene Decentraverse.slnx, risalendo da quella corrente.</summary>
    public static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Decentraverse.slnx")))
            {
                return dir.FullName;
            }
        }
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Lettore minimale di .env: righe CHIAVE=valore, commenti con #, virgolette facoltative.
    /// Le variabili già presenti nell'ambiente hanno la precedenza.
    /// </summary>
    private static void LoadEnvFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            int eq = line.IndexOf('=');
            if (line.Length == 0 || line.StartsWith('#') || eq <= 0)
            {
                continue;
            }

            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim().Trim('"', '\'');
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
