using System.Globalization;
using System.Text.Json;
using Decentraverse.Indexer;

const string Usage = """
    Uso: dotnet run --project src/Decentraverse.Indexer -- <comando> [opzioni]

    Comandi:
      backfill --count N            scarica gli ultimi N blocchi finalizzati (default 1000)
      watch [--interval S]          polling dei nuovi blocchi finalizzati (default ogni 30s)
      serve [--watch] [--port P]    avvia l'API HTTP (con --watch fa anche il polling)
      export --count N --out FILE   esporta gli ultimi N blocchi del database in JSON (fixture offline)
      fetch --numbers A,B,C --out FILE
                                    scarica blocchi specifici dal nodo in JSON (fixture dei golden test)
      system --number N [--moons]   stampa semi e sistema generato di un blocco, in JSON (verifica a mano)
    """;

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    Console.WriteLine(Usage);
    return 0;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var settings = Settings.Load();
    var store = new BlockStore(settings.DatabasePath);
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var rpc = new EthRpcClient(http, settings.RpcUrl);
    var sync = new Sync(rpc, store);
    var options = Options.Parse(args.AsSpan(1));

    switch (args[0])
    {
        case "backfill":
            await sync.BackfillAsync(options.Int("count", 1_000), cts.Token);
            break;

        case "watch":
            await sync.WatchAsync(TimeSpan.FromSeconds(options.Int("interval", 30)), cts.Token);
            break;

        case "serve":
        {
            int port = options.Int("port", settings.ApiPort);
            var app = Api.Build(store, port);
            Task watch = options.Flag("watch")
                ? sync.WatchAsync(TimeSpan.FromSeconds(options.Int("interval", 30)), cts.Token)
                : Task.CompletedTask;
            Log.Info($"API in ascolto su http://localhost:{port} ({store.Count()} blocchi nel database). Ctrl+C per fermare.");
            await app.RunAsync(cts.Token);
            cts.Cancel();
            await watch;
            break;
        }

        case "export":
        {
            var blocks = store.Range(null, options.Int("count", 500));
            WriteJson(options.Required("out"), blocks);
            Log.Info($"Esportati {blocks.Count} blocchi.");
            break;
        }

        case "fetch":
        {
            var numbers = options.Required("numbers").Split(',')
                .Select(s => long.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToArray();
            var blocks = await rpc.GetBlocksAsync(numbers, cts.Token);
            WriteJson(options.Required("out"), blocks);
            Log.Info($"Scaricati {blocks.Count} blocchi.");
            break;
        }

        case "system":
        {
            long number = long.Parse(options.Required("number"), CultureInfo.InvariantCulture);
            var block = store.Get(number) ?? (await rpc.GetBlocksAsync([number], cts.Token))[0];
            Console.WriteLine(SystemReport.Build(block, options.Flag("moons")));
            break;
        }

        default:
            Console.Error.WriteLine($"Comando sconosciuto: {args[0]}\n\n{Usage}");
            return 2;
    }

    return 0;
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    return 130;
}
catch (Exception ex) when (ex is RpcException or InvalidOperationException or ArgumentException or FormatException)
{
    Log.Warn(ex.Message);
    return 1;
}

static void WriteJson(string path, IReadOnlyList<BlockRecord> blocks)
{
    // Relative paths start from the repo root, whatever the current folder is.
    string full = Path.GetFullPath(path, Settings.FindRepoRoot());
    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
    File.WriteAllText(full, JsonSerializer.Serialize(blocks, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }) + "\n");
}

/// <summary>Minimal parser for --name value and --flag options.</summary>
internal sealed class Options(Dictionary<string, string?> values)
{
    public static Options Parse(ReadOnlySpan<string> args)
    {
        var values = new Dictionary<string, string?>();
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Argomento inatteso: {args[i]}");
            }

            string name = args[i][2..];
            bool hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            values[name] = hasValue ? args[++i] : null;
        }
        return new Options(values);
    }

    public bool Flag(string name) => values.ContainsKey(name);

    public int Int(string name, int fallback)
    {
        if (!values.TryGetValue(name, out var raw))
        {
            return fallback;
        }
        if (raw is null || !int.TryParse(raw, CultureInfo.InvariantCulture, out int value) || value <= 0)
        {
            throw new ArgumentException($"--{name} richiede un intero positivo.");
        }
        return value;
    }

    public string Required(string name) =>
        values.TryGetValue(name, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw
            : throw new ArgumentException($"Manca l'opzione --{name}.");
}
