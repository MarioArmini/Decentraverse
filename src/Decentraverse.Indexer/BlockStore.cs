using Microsoft.Data.Sqlite;

namespace Decentraverse.Indexer;

/// <summary>
/// Archivio SQLite dei blocchi. Ogni operazione apre la propria connessione (il pool la riusa),
/// così l'API può leggere mentre il watch scrive; la modalità WAL permette letture concorrenti.
/// </summary>
public sealed class BlockStore
{
    private readonly string _connectionString;

    public BlockStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();

        using var conn = Open();
        Execute(conn, """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS blocks (
                number    INTEGER PRIMARY KEY,
                hash      TEXT    NOT NULL UNIQUE,
                timestamp INTEGER NOT NULL,
                tx_count  INTEGER NOT NULL,
                gas_used  INTEGER NOT NULL
            );
            """);
    }

    /// <summary>
    /// Inserisce i blocchi mancanti e restituisce quanti sono nuovi. Un blocco già presente con un hash
    /// diverso è un errore grave (con i blocchi finalizzati non deve mai succedere): si interrompe tutto.
    /// </summary>
    public int Insert(IEnumerable<BlockRecord> blocks)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using var insert = conn.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = """
            INSERT INTO blocks (number, hash, timestamp, tx_count, gas_used)
            VALUES ($number, $hash, $timestamp, $txCount, $gasUsed)
            ON CONFLICT(number) DO NOTHING;
            """;
        var pNumber = insert.Parameters.Add("$number", SqliteType.Integer);
        var pHash = insert.Parameters.Add("$hash", SqliteType.Text);
        var pTimestamp = insert.Parameters.Add("$timestamp", SqliteType.Integer);
        var pTxCount = insert.Parameters.Add("$txCount", SqliteType.Integer);
        var pGasUsed = insert.Parameters.Add("$gasUsed", SqliteType.Integer);

        int inserted = 0;
        foreach (var b in blocks)
        {
            pNumber.Value = b.Number;
            pHash.Value = b.Hash;
            pTimestamp.Value = b.Timestamp;
            pTxCount.Value = b.TxCount;
            pGasUsed.Value = b.GasUsed;

            if (insert.ExecuteNonQuery() == 1)
            {
                inserted++;
                continue;
            }

            var existing = Get(conn, tx, b.Number);
            if (existing is not null && existing.Hash != b.Hash)
            {
                throw new InvalidOperationException(
                    $"Il blocco {b.Number} è già salvato con hash {existing.Hash}, ma il nodo ora dice {b.Hash}.");
            }
        }

        tx.Commit();
        return inserted;
    }

    public long? MaxNumber() => Scalar("SELECT MAX(number) FROM blocks;");

    public long Count() => Scalar("SELECT COUNT(*) FROM blocks;") ?? 0;

    /// <summary>Numeri già salvati nell'intervallo [from, to].</summary>
    public HashSet<long> ExistingNumbers(long from, long to)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT number FROM blocks WHERE number BETWEEN $from AND $to;";
        cmd.Parameters.AddWithValue("$from", from);
        cmd.Parameters.AddWithValue("$to", to);

        var result = new HashSet<long>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(reader.GetInt64(0));
        }
        return result;
    }

    public BlockRecord? Get(long number)
    {
        using var conn = Open();
        return Get(conn, null, number);
    }

    public BlockRecord? Latest() => Query("SELECT * FROM blocks ORDER BY number DESC LIMIT 1;").FirstOrDefault();

    /// <summary>
    /// Blocchi in ordine crescente. Con <paramref name="from"/>: da quel numero in su.
    /// Senza: gli ultimi <paramref name="limit"/> blocchi.
    /// </summary>
    public IReadOnlyList<BlockRecord> Range(long? from, int limit)
    {
        if (from is long start)
        {
            return Query("SELECT * FROM blocks WHERE number >= $from ORDER BY number ASC LIMIT $limit;",
                ("$from", start), ("$limit", limit));
        }

        var latest = Query("SELECT * FROM blocks ORDER BY number DESC LIMIT $limit;", ("$limit", limit));
        return latest.Reverse().ToArray();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static void Execute(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private long? Scalar(string sql)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar() is long value ? value : null;
    }

    private static BlockRecord? Get(SqliteConnection conn, SqliteTransaction? tx, long number)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT * FROM blocks WHERE number = $number;";
        cmd.Parameters.AddWithValue("$number", number);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    private IReadOnlyList<BlockRecord> Query(string sql, params (string Name, object Value)[] parameters)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        var result = new List<BlockRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(Read(reader));
        }
        return result;
    }

    private static BlockRecord Read(SqliteDataReader r) => new(
        r.GetInt64(r.GetOrdinal("number")),
        r.GetString(r.GetOrdinal("hash")),
        r.GetInt64(r.GetOrdinal("timestamp")),
        r.GetInt32(r.GetOrdinal("tx_count")),
        r.GetInt64(r.GetOrdinal("gas_used")));
}
