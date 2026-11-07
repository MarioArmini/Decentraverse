namespace Decentraverse.Indexer;

/// <summary>
/// Read-only HTTP API (ASP.NET Core Minimal API, part of the framework).
///   GET /blocks?from=&amp;limit=   blocks in ascending order (without from: the last `limit`)
///   GET /blocks/{number}        one block, 404 if missing
///   GET /latest                 the latest stored block, 404 if the database is empty
/// </summary>
public static class Api
{
    public const int DefaultLimit = 1_000;
    public const int MaxLimit = 10_000;

    public static WebApplication Build(BlockStore store, int port)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // The frontend runs on another port, so CORS must be allowed. The data is public and read-only.
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().WithMethods("GET")));

        var app = builder.Build();
        app.UseCors();

        app.MapGet("/blocks", (long? from, int? limit) =>
        {
            int take = limit ?? DefaultLimit;
            if (take < 1 || take > MaxLimit)
            {
                return Results.BadRequest(new { error = $"limit deve essere tra 1 e {MaxLimit}." });
            }
            return Results.Ok(store.Range(from, take));
        });

        app.MapGet("/blocks/{number:long}", (long number) =>
            store.Get(number) is { } block ? Results.Ok(block) : Results.NotFound(new { error = $"Blocco {number} non indicizzato." }));

        app.MapGet("/latest", () =>
            store.Latest() is { } block ? Results.Ok(block) : Results.NotFound(new { error = "Database vuoto: lancia prima il backfill." }));

        return app;
    }
}
