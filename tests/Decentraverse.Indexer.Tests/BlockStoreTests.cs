using Decentraverse.Indexer;

namespace Decentraverse.Indexer.Tests;

public sealed class BlockStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "decentraverse-tests-" + Guid.NewGuid().ToString("N"));
    private readonly BlockStore _store;

    public BlockStoreTests() => _store = new BlockStore(Path.Combine(_dir, "blocks.db"));

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, recursive: true);
    }

    private static BlockRecord Block(long n) => new(n, "0x" + n.ToString("x64"), 1_700_000_000 + n * 12, (int)(n % 300), n * 1_000);

    [Fact]
    public void InsertIsIdempotentAndQueriesWork()
    {
        Assert.Null(_store.MaxNumber());
        Assert.Null(_store.Latest());

        Assert.Equal(5, _store.Insert(Enumerable.Range(10, 5).Select(i => Block(i))));
        Assert.Equal(0, _store.Insert([Block(12)]));

        Assert.Equal(5, _store.Count());
        Assert.Equal(14, _store.MaxNumber());
        Assert.Equal(Block(12), _store.Get(12));
        Assert.Null(_store.Get(99));
        Assert.Equal(Block(14), _store.Latest());
        Assert.Equal([11L, 12L], _store.ExistingNumbers(0, 12).Where(n => n > 10).Order());
    }

    [Fact]
    public void RangeIsAscendingWithAndWithoutFrom()
    {
        _store.Insert(Enumerable.Range(1, 10).Select(i => Block(i)));

        Assert.Equal([4L, 5L, 6L], _store.Range(4, 3).Select(b => b.Number));
        Assert.Equal([8L, 9L, 10L], _store.Range(null, 3).Select(b => b.Number));
    }

    [Fact]
    public void ConflictingHashForSameNumberIsRejected()
    {
        _store.Insert([Block(1)]);
        Assert.Throws<InvalidOperationException>(() => _store.Insert([Block(1) with { Hash = "0x" + new string('f', 64) }]));
    }
}
