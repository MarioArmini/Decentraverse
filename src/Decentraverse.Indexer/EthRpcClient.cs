using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Decentraverse.Indexer;

/// <summary>
/// Client JSON-RPC minimale per Ethereum, solo in lettura.
/// Usa solo eth_getBlockByNumber e gestisce rate limit e errori temporanei con backoff esponenziale.
/// Non scrive mai l'URL nei log: con Infura contiene la chiave.
/// </summary>
public sealed class EthRpcClient(HttpClient http, Uri endpoint)
{
    public int MaxAttempts { get; init; } = 8;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(60);

    // Codici JSON-RPC che indicano un limite di richieste (Infura usa -32005).
    private static readonly HashSet<int> RateLimitCodes = [-32005, -32029, -32090, 429];

    /// <summary>L'ultimo blocco finalizzato: non può più essere annullato da una riorganizzazione della catena.</summary>
    public async Task<BlockRecord> GetFinalizedBlockAsync(CancellationToken ct)
    {
        var results = await CallAsync([BuildRequest(0, "finalized")], ct);
        return ParseBlock(results[0]);
    }

    /// <summary>Più blocchi in una sola richiesta HTTP (batch JSON-RPC).</summary>
    public async Task<IReadOnlyList<BlockRecord>> GetBlocksAsync(IReadOnlyList<long> numbers, CancellationToken ct)
    {
        if (numbers.Count == 0)
        {
            return [];
        }

        var requests = numbers.Select((n, i) => BuildRequest(i, "0x" + n.ToString("x", CultureInfo.InvariantCulture))).ToArray();
        var results = await CallAsync(requests, ct);
        return results.Select(ParseBlock).ToArray();
    }

    private static JsonObject BuildRequest(int id, string blockTag) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["method"] = "eth_getBlockByNumber",
        // false = transazioni come lista di hash: basta per contarle e la risposta è molto più leggera.
        ["params"] = new JsonArray(blockTag, false),
    };

    /// <summary>Invia le richieste (singola o batch) con retry; restituisce i "result" nell'ordine delle richieste.</summary>
    private async Task<JsonNode[]> CallAsync(JsonObject[] requests, CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            TimeSpan? retryAfter = null;
            string problem;
            try
            {
                JsonNode body = requests.Length == 1 ? requests[0].DeepClone() : new JsonArray(requests.Select(r => r.DeepClone()).ToArray());
                using var response = await http.PostAsJsonAsync(endpoint, body, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                {
                    retryAfter = response.Headers.RetryAfter?.Delta;
                    problem = $"HTTP {(int)response.StatusCode}";
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadFromJsonAsync<JsonNode>(ct)
                        ?? throw new RpcException("Risposta vuota dal nodo.");
                    return ExtractResults(json, requests.Length);
                }
            }
            catch (RateLimitedException ex)
            {
                problem = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                problem = $"errore di rete ({ex.HttpRequestError})";
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                problem = "timeout";
            }

            if (attempt >= MaxAttempts)
            {
                throw new RpcException($"Il nodo non risponde dopo {attempt} tentativi: {problem}.");
            }

            TimeSpan delay = retryAfter ?? Backoff(attempt);
            Log.Warn($"RPC {endpoint.Host}: {problem}. Nuovo tentativo {attempt + 1}/{MaxAttempts} tra {delay.TotalSeconds:F1}s.");
            await Task.Delay(delay, ct);
        }
    }

    /// <summary>Backoff esponenziale con jitter: 1s, 2s, 4s, ... fino a MaxDelay, ±25% casuale.</summary>
    private TimeSpan Backoff(int attempt)
    {
        double seconds = Math.Min(MaxDelay.TotalSeconds, BaseDelay.TotalSeconds * Math.Pow(2, attempt - 1));
        double jitter = 0.75 + Random.Shared.NextDouble() * 0.5;
        return TimeSpan.FromSeconds(seconds * jitter);
    }

    internal static JsonNode[] ExtractResults(JsonNode json, int expected)
    {
        // Un batch rifiutato in blocco può tornare come singolo oggetto di errore invece che come array.
        var items = json is JsonArray array ? array.ToArray() : [json];
        if (items.Length == 1 && expected > 1)
        {
            ThrowIfError(items[0]!);
            throw new RpcException($"Attese {expected} risposte nel batch, ricevuta 1.");
        }

        var results = new JsonNode[expected];
        foreach (var item in items)
        {
            ThrowIfError(item!);
            int id = item!["id"]!.GetValue<int>();
            results[id] = item["result"] ?? throw new RpcException($"Il nodo non conosce il blocco richiesto (id {id}).");
        }

        if (results.Any(r => r is null))
        {
            throw new RpcException("Il batch di risposta è incompleto.");
        }

        return results;
    }

    private static void ThrowIfError(JsonNode item)
    {
        if (item["error"] is not JsonNode error)
        {
            return;
        }

        int code = error["code"]?.GetValue<int>() ?? 0;
        string message = error["message"]?.GetValue<string>() ?? "errore sconosciuto";
        if (RateLimitCodes.Contains(code) || message.Contains("rate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("limit", StringComparison.OrdinalIgnoreCase))
        {
            throw new RateLimitedException($"limite di richieste ({code}: {message})");
        }

        throw new RpcException($"Errore JSON-RPC {code}: {message}");
    }

    internal static BlockRecord ParseBlock(JsonNode block)
    {
        string hash = block["hash"]!.GetValue<string>();
        if (hash.Length != 66 || !hash.StartsWith("0x", StringComparison.Ordinal))
        {
            throw new RpcException($"Hash di blocco malformato: {hash}");
        }

        return new BlockRecord(
            Number: Quantity(block["number"]!),
            Hash: hash.ToLowerInvariant(),
            Timestamp: Quantity(block["timestamp"]!),
            TxCount: block["transactions"]!.AsArray().Count,
            GasUsed: Quantity(block["gasUsed"]!));
    }

    /// <summary>Le quantità JSON-RPC sono stringhe esadecimali, es. "0x1b4".</summary>
    private static long Quantity(JsonNode node)
    {
        string s = node.GetValue<string>();
        if (!s.StartsWith("0x", StringComparison.Ordinal))
        {
            throw new RpcException($"Quantità non esadecimale: {s}");
        }
        return checked((long)ulong.Parse(s.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
    }
}

public class RpcException(string message) : Exception(message);

internal sealed class RateLimitedException(string message) : RpcException(message);
