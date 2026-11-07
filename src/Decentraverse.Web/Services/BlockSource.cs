using System.Net.Http.Json;
using Decentraverse.Generator;

namespace Decentraverse.Web.Services;

public enum SourceMode { Loading, Live, Offline }

/// <summary>
/// Supplies blocks to the UI: from the indexer API when it answers, otherwise from the
/// bundled fixture (≈500 real blocks), so the demo always works.
/// </summary>
public sealed class BlockSource(HttpClient localHttp, IConfiguration config)
{
    private readonly HttpClient _api = new() { BaseAddress = new Uri(config["ApiBaseUrl"] ?? "http://localhost:8787") };
    private readonly int _initialCount = int.TryParse(config["InitialBlockCount"], out int n) ? n : 1000;

    public SourceMode Mode { get; private set; } = SourceMode.Loading;

    public string ApiBaseUrl => _api.BaseAddress!.ToString().TrimEnd('/');

    public TimeSpan PollInterval { get; } = TimeSpan.FromSeconds(int.TryParse(config["PollSeconds"], out int s) ? s : 30);

    /// <summary>Last blocks from the API; on any failure, the fixture.</summary>
    public async Task<IReadOnlyList<BlockInput>> LoadInitialAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var blocks = await _api.GetFromJsonAsync<List<BlockInput>>($"blocks?limit={_initialCount}", timeout.Token);
            if (blocks is { Count: > 0 })
            {
                Mode = SourceMode.Live;
                return blocks;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            Console.WriteLine($"Indexer API not reachable ({ex.GetType().Name}): using the offline fixture.");
        }

        Mode = SourceMode.Offline;
        return await localHttp.GetFromJsonAsync<List<BlockInput>>("data/fixture-blocks.json") ?? [];
    }

    /// <summary>Blocks newer than <paramref name="afterNumber"/>; empty when offline or on errors.</summary>
    public async Task<IReadOnlyList<BlockInput>> FetchNewerAsync(long afterNumber)
    {
        if (Mode != SourceMode.Live)
        {
            return [];
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return await _api.GetFromJsonAsync<List<BlockInput>>($"blocks?from={afterNumber + 1}&limit=1000", timeout.Token) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            Console.WriteLine($"Polling failed ({ex.GetType().Name}); will retry.");
            return [];
        }
    }
}
