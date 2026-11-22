using Core.Import;
using Core.Services;

namespace Cli;

// Експорт усієї колекції замовлень у JSON (команда --export).
internal static class ExportMode
{
    public static async Task<int> RunAsync(string[] args)
    {
        var service = new OrderService(StoreFactory.Create(args));
        var all = service.All();

        await JsonOrderExporter.ExportAsync(all, AppSettings.ExportFilePath);
        Console.WriteLine($"Експортовано {all.Count} замовлень у {AppSettings.ExportFilePath}");
        return 0;
    }
}
