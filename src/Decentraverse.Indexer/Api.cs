namespace Decentraverse.Indexer;

/// <summary>
/// API HTTP di sola lettura (ASP.NET Core Minimal API, incluso nel framework).
///   GET /blocks?from=&amp;limit=   blocchi in ordine crescente (senza from: gli ultimi `limit`)
///   GET /blocks/{number}        un blocco, 404 se assente
///   GET /latest                 l'ultimo blocco salvato, 404 se il database è vuoto
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

        // Il frontend gira su un'altra porta: va permesso il CORS. I dati sono pubblici e in sola lettura.
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
