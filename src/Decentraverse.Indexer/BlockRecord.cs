namespace Decentraverse.Indexer;

/// <summary>
/// The only data stored for each block. Systems are never stored: they are regenerated.
/// Serialized in camelCase: { number, hash, timestamp, txCount, gasUsed }.
/// </summary>
public sealed record BlockRecord(long Number, string Hash, long Timestamp, int TxCount, long GasUsed);
