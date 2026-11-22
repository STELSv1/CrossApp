using Core;
using Core.Import;
using Core.Services;

namespace Cli;

// Лабораторна 8: асинхронний імпорт.
// Прапорці: --stream (потоковий режим), --timeout <сек> (таймаут).
// Скасування: Ctrl+C або таймаут, об'єднані через CreateLinkedTokenSource.
internal static class AsyncImportMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        string path = args.FirstOrDefault(a => a.EndsWith(".json")) ?? Path.Combine("data", "orders.json");
        bool stream = args.Contains("--stream");
        int? seconds = ReadTimeout(args);

        using var userCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; userCts.Cancel(); };

        using var timeoutCts = new CancellationTokenSource();
        if (seconds is int s)
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(s));

        // Один токен на операцію: скасовується або користувачем, або таймаутом.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        var service = new OrderService(StoreFactory.Create(args));
        var progress = new ConsoleProgress();

        try
        {
            int added = stream
                ? await service.ImportStreamAsync(new JsonOrderImporter(), path, linked.Token)
                : await service.ImportAsync(new JsonOrderImporter(), path, linked.Token, progress);

            Console.WriteLine($"Імпортовано записів: {added} ({(stream ? "потоково" : "звичайно")})");
            Console.WriteLine($"Усього в сховищі: {service.All().Count}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            // Розрізняємо причину: таймаут чи користувач.
            Console.WriteLine(timeoutCts.IsCancellationRequested
                ? $"Імпорт скасовано: таймаут {seconds} с."
                : "Імпорт скасовано користувачем (Ctrl+C).");
            return 2;
        }
        catch (Core.DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 1;
        }
       catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}
        finally
        {
            progress.Finish();
        }
    }

    private static int? ReadTimeout(string[] args)
    {
        int i = Array.IndexOf(args, "--timeout");
        return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int s) ? s : null;
    }

    // Синхронний прогрес для консолі: вивід відбувається в тому самому потоці, без черги.
    private sealed class ConsoleProgress : IProgress<int>
    {
        private bool _shown;

        public void Report(int percent)
        {
            Console.Write($"\r  прогрес: {percent,3}%");
            _shown = true;
        }

        public void Finish()
        {
            if (_shown)
            {
                Console.WriteLine();
                _shown = false;
            }
        }
    }
}
