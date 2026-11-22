using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Фабрика сховищ. Шлях до файлу береться з конфігурації (AppSettings), а не зашитий у коді.
internal static class StoreFactory
{
    public static string DataPath => AppSettings.DataFilePath;

    public static IOrderStore Create(string[] args)
    {
        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(DataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        return args.Contains("--cache") ? new Core.Storage.CachingOrderStore(store) : store;
    }
}
