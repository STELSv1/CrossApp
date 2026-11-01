using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Додаткове завдання 3: фабрика сховищ. Вся логіка вибору реалізації — в одному місці,
// Program.cs лише викликає Create(args).
internal static class StoreFactory
{
    public static string DataPath => Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

    public static IOrderStore Create(string[] args)
    {
        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(DataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }
}
