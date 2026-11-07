namespace Decentraverse.Indexer;

internal static class Log
{
    public static void Info(string message) => Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");

    public static void Warn(string message) => Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss} ⚠ {message}");
}
