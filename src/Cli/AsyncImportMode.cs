using Core;
using Core.Import;
using Core.Services;

namespace Cli;

// Лабораторна 8: асинхронний імпорт з Ctrl+C і --timeout <сек>.
internal static class AsyncImportMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        string path = args.FirstOrDefault(a => a.EndsWith(".json")) ?? Path.Combine("data", "orders.json");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        int timeoutIndex = Array.IndexOf(args, "--timeout");
        if (timeoutIndex >= 0 && timeoutIndex + 1 < args.Length &&
            int.TryParse(args[timeoutIndex + 1], out int seconds))
        {
            cts.CancelAfter(TimeSpan.FromSeconds(seconds));
        }

        var service = new OrderService(StoreFactory.Create(args));
        try
        {
            int added = await service.ImportAsync(new JsonOrderImporter(), path, cts.Token);
            Console.WriteLine($"Імпортовано записів: {added}");
            Console.WriteLine($"Усього в сховищі: {service.All().Count}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Імпорт скасовано.");
            return 2;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 1;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }
    }
}
