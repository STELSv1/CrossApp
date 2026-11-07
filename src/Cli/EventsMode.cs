using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;

namespace Cli;

// Лабораторна 6: підписки на події, відписка, відмови, делегати Func і Action.
internal static class EventsMode
{
    public static int Run(string[] args)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
        IOrderStore store = StoreFactory.Create(args);
        var service = new OrderService(store);
        var sinks = new INotificationSink[] { new ConsoleSink(), new FileSink(logPath) };

        Console.WriteLine($"Сховище: {store.GetType().Name}");
        Console.WriteLine($"Лог: {Path.GetFullPath(logPath)}");
        Console.WriteLine();

        // Підписка 1: локальний метод (його можна відписати через -=)
        void OnOrderChanged(object? sender, OrderChangedEventArgs e)
        {
            foreach (INotificationSink sink in sinks)
                sink.Notify(e);
        }
        service.Changed += OnOrderChanged;

        // Підписка 2: лямбда із замиканням над локальною змінною changes
        int changes = 0;
        service.Changed += (_, _) => changes++;

        Console.WriteLine("--- Успішні операції: події виводяться і пишуться в лог ---");
        string id = $"ORD-{Guid.NewGuid():N}"[..12];
        service.CreateOrder(id, "CU-001");
        service.AddLine(id, "P-001", "Цемент М400 25кг", 185.50m, 10);
        service.AddLine(id, "P-005", "Шпаклівка фінішна", 58.00m, 4);
        service.Confirm(id);
        Console.WriteLine($"Подій отримано: {changes}");

        Console.WriteLine();
        Console.WriteLine("--- Після відписки -= : повідомлень немає, лічильник лямбди працює ---");
        service.Changed -= OnOrderChanged;
        string id2 = $"ORD-{Guid.NewGuid():N}"[..12];
        service.CreateOrder(id2, "CU-002");
        service.AddLine(id2, "P-002", "Пісок будівельний", 420.00m, 2);
        Console.WriteLine($"Подій отримано: {changes}");

        Console.WriteLine();
        Console.WriteLine("--- Сценарій відмови: виняток є, події немає ---");
        int before = changes;
        TryDo("рядок у неіснуюче замовлення", () => service.AddLine("ORD-NOPE", "P-001", "Цемент", 185.50m, 1));
        TryDo("від'ємна кількість", () => service.AddLine(id2, "P-002", "Пісок", 420.00m, -1));
        Console.WriteLine($"Подій після відмов: {changes - before} (очікується 0)");

        Console.WriteLine();
        Console.WriteLine("--- Делегат Func: пошук за умовою (подія тут не підходить, бо потрібен результат) ---");
        IReadOnlyList<Order> big = service.Search(o => o.Total > 1000m);
        Console.WriteLine($"Замовлень на суму більше 1000: {big.Count}");

        Console.WriteLine();
        Console.WriteLine("--- Делегат Action: звіт про пропущені рядки імпорту ---");
        string csv = Path.Combine("data", "sample.csv");
        if (File.Exists(csv))
        {
            ImportResult<ProductDto> result = ProductCsvImporter.Load(csv, msg => Console.WriteLine($"  пропуск -> {msg}"));
            Console.WriteLine($"Завантажено товарів: {result.Items.Count}, пропущено: {result.Errors.Count}");
        }
        else
        {
            Console.WriteLine($"Файл {Path.GetFullPath(csv)} не знайдено, запустіть з кореня проєкту.");
        }

        return 0;
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював!");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}
