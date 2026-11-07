using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Decentraverse.Indexer;

namespace Decentraverse.Indexer.Tests;

public class EthRpcClientTests
{
    private const string BlockJson = """
        {"number":"0x1924157","hash":"0x2BEA7E52F59C877A2ECC0D250ED7C442F38826752BB7A8C6B69021C62FB64911",
         "timestamp":"0x6aeef4d7","gasUsed":"0x222dd59","transactions":["0x01","0x02","0x03"]}
        """;

    [Fact]
    public void ParsesHexQuantitiesAndCountsTransactions()
    {
        var block = EthRpcClient.ParseBlock(JsonNode.Parse(BlockJson)!);

        Assert.Equal(26362199, block.Number);
        Assert.Equal("0x2bea7e52f59c877a2ecc0d250ed7c442f38826752bb7a8c6b69021c62fb64911", block.Hash);
        Assert.Equal(1794045143, block.Timestamp);
        Assert.Equal(3, block.TxCount);
        Assert.Equal(35839321, block.GasUsed);
    }

    [Fact]
    public void BatchResultsAreMatchedById()
    {
        var json = JsonNode.Parse("""[{"id":1,"result":"b"},{"id":0,"result":"a"}]""")!;
        var results = EthRpcClient.ExtractResults(json, 2);
        Assert.Equal("a", results[0].GetValue<string>());
        Assert.Equal("b", results[1].GetValue<string>());
    }

    [Fact]
    public void MissingBlockAndRpcErrorsThrow()
    {
        Assert.Throws<RpcException>(() => EthRpcClient.ExtractResults(JsonNode.Parse("""{"id":0,"result":null}""")!, 1));
        Assert.Throws<RpcException>(() => EthRpcClient.ExtractResults(
            JsonNode.Parse("""{"id":0,"error":{"code":-32602,"message":"invalid params"}}""")!, 1));
        Assert.Throws<RpcException>(() => EthRpcClient.ExtractResults(JsonNode.Parse("""[{"id":0,"result":"a"}]""")!, 2));
    }

    [Fact]
    public async Task RetriesOnRateLimitThenSucceeds()
    {
        var handler = new ScriptedHandler(
            () => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            () => Json("""{"jsonrpc":"2.0","id":0,"error":{"code":-32005,"message":"daily request count exceeded"}}"""),
            () => new HttpResponseMessage(HttpStatusCode.BadGateway),
            () => Json($$"""{"jsonrpc":"2.0","id":0,"result":{{BlockJson}}}"""));

        var client = new EthRpcClient(new HttpClient(handler), new Uri("https://rpc.example/key"))
        {
            BaseDelay = TimeSpan.FromMilliseconds(1),
        };

        var block = await client.GetFinalizedBlockAsync(CancellationToken.None);
        Assert.Equal(26362199, block.Number);
        Assert.Equal(4, handler.Calls);
    }

    [Fact]
    public async Task GivesUpAfterMaxAttempts()
    {
        var handler = new ScriptedHandler(Enumerable.Repeat<Func<HttpResponseMessage>>(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), 3).ToArray());
        var client = new EthRpcClient(new HttpClient(handler), new Uri("https://rpc.example/key"))
        {
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxAttempts = 3,
        };

        await Assert.ThrowsAsync<RpcException>(() => client.GetFinalizedBlockAsync(CancellationToken.None));
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task DoesNotRetryNonTransientErrors()
    {
        var handler = new ScriptedHandler(
            () => Json("""{"jsonrpc":"2.0","id":0,"error":{"code":-32602,"message":"invalid params"}}"""));
        var client = new EthRpcClient(new HttpClient(handler), new Uri("https://rpc.example/key"));

        await Assert.ThrowsAsync<RpcException>(() => client.GetFinalizedBlockAsync(CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class ScriptedHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(responses[Calls++]());
    }
}
