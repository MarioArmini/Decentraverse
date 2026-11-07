namespace Decentraverse.Indexer;

/// <summary>Downloads finalized blocks from the node and stores them. Used by backfill and watch.</summary>
public sealed class Sync(EthRpcClient rpc, BlockStore store)
{
    public int BatchSize { get; init; } = 20;

    /// <summary>Downloads the last <paramref name="count"/> finalized blocks, skipping those already stored.</summary>
    public async Task<int> BackfillAsync(int count, CancellationToken ct)
    {
        var finalized = await rpc.GetFinalizedBlockAsync(ct);
        long from = Math.Max(0, finalized.Number - count + 1);
        Log.Info($"Ultimo blocco finalizzato: {finalized.Number}. Backfill di {from}…{finalized.Number}.");

        int inserted = store.Insert([finalized]);
        inserted += await FetchRangeAsync(from, finalized.Number - 1, ct);
        Log.Info($"Backfill completato: {inserted} blocchi nuovi, {store.Count()} in totale nel database.");
        return inserted;
    }

    /// <summary>
    /// One polling round: brings the database up to the latest finalized block.
    /// On first start with an empty database it begins at the latest finalized block (use backfill for history).
    /// </summary>
    public async Task<int> CatchUpAsync(CancellationToken ct)
    {
        var finalized = await rpc.GetFinalizedBlockAsync(ct);
        long? max = store.MaxNumber();

        int inserted = 0;
        if (max is long last && last + 1 < finalized.Number)
        {
            inserted += await FetchRangeAsync(last + 1, finalized.Number - 1, ct);
        }
        inserted += store.Insert([finalized]);
        return inserted;
    }

    /// <summary>Runs CatchUp at regular intervals; on failure it doubles the wait, up to 10 minutes.</summary>
    public async Task WatchAsync(TimeSpan interval, CancellationToken ct)
    {
        Log.Info($"Watch avviato: controllo ogni {interval.TotalSeconds:F0}s (i blocchi si finalizzano a gruppi di 32, circa ogni 6,4 minuti).");
        TimeSpan wait = interval;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                int inserted = await CatchUpAsync(ct);
                if (inserted > 0)
                {
                    Log.Info($"+{inserted} blocchi finalizzati (ultimo: {store.MaxNumber()}).");
                }
                wait = interval;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is RpcException or HttpRequestException or System.Text.Json.JsonException)
            {
                wait = TimeSpan.FromSeconds(Math.Min(wait.TotalSeconds * 2, 600));
                Log.Warn($"Watch: {ex.Message} Riprovo tra {wait.TotalSeconds:F0}s.");
            }

            try
            {
                await Task.Delay(wait, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Log.Info("Watch fermato.");
    }

    private async Task<int> FetchRangeAsync(long from, long to, CancellationToken ct)
    {
        if (to < from)
        {
            return 0;
        }

        var existing = store.ExistingNumbers(from, to);
        var missing = new List<long>();
        for (long n = from; n <= to; n++)
        {
            if (!existing.Contains(n))
            {
                missing.Add(n);
            }
        }

        int inserted = 0;
        for (int i = 0; i < missing.Count; i += BatchSize)
        {
            var chunk = missing.GetRange(i, Math.Min(BatchSize, missing.Count - i));
            inserted += store.Insert(await rpc.GetBlocksAsync(chunk, ct));

            int done = Math.Min(i + BatchSize, missing.Count);
            if (missing.Count > BatchSize && (done % (BatchSize * 10) == 0 || done == missing.Count))
            {
                Log.Info($"  {done}/{missing.Count} blocchi scaricati");
            }
        }

        return inserted;
    }
}
