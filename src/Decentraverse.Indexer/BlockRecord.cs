namespace Decentraverse.Indexer;

/// <summary>
/// Gli unici dati salvati per ogni blocco. I sistemi non si salvano: si rigenerano.
/// Serializzato in camelCase: { number, hash, timestamp, txCount, gasUsed }.
/// </summary>
public sealed record BlockRecord(long Number, string Hash, long Timestamp, int TxCount, long GasUsed);
