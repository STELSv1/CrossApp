using Cli;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Режими лаб 1–4 (окремі прапорці). Без прапорця — лабораторна 5.
if (args.Contains("--env")) return LegacyModes.RunEnvironmentReport(args.Contains("--json"));
if (args.Contains("--domain")) return LegacyModes.RunDomainDemo();
if (args.Contains("--mixed")) return LegacyModes.RunMixedImport(args);
if (args.Contains("--import")) return LegacyModes.RunImport(args);
if (args.Contains("--events")) return EventsMode.Run(args);

return RunCatalogScenario(args);

// Лабораторна 5. Вибір сховища — у StoreFactory (composition root з правилами вибору).
static int RunCatalogScenario(string[] args)
{
    IOrderStore store = StoreFactory.Create(args);
    var service = new OrderService(store);

    try
    {
        Console.WriteLine($"Сховище: {store.GetType().Name}");
        if (args.Contains("--file"))
            Console.WriteLine($"Файл: {Path.GetFullPath(StoreFactory.DataPath)}");

        // Додати, змінити, показати список, знайти за id
        string newId = $"ORD-{Guid.NewGuid():N}"[..12];
        service.CreateOrder(newId, "CU-001");
        service.AddLine(newId, "P-001", "Цемент М400 25кг", 185.50m, 10);
        service.AddLine(newId, "P-005", "Шпаклівка фінішна", 58.00m, 4);
        service.Confirm(newId);

        IReadOnlyList<Order> all = service.All();
        Console.WriteLine($"Записів у сховищі: {all.Count}");
        foreach (Order o in all.Take(5))
            Console.WriteLine($"  {o.Id,-12} {o.CustomerId,-8} {o.Status,-10} рядків: {o.Lines.Count,2}  сума: {o.Total,12:F2}");

        Order? found = service.Find(newId);
        Console.WriteLine(found is null ? $"Не знайдено: {newId}" : $"Знайдено за id: {found}");

        // Додаткове завдання 2: пошук з предикатом
        IReadOnlyList<Order> multi = service.Search(o => o.IsConfirmed && o.Lines.Count > 1);
        Console.WriteLine($"Пошук (підтверджені з більш ніж одним рядком): знайдено {multi.Count}");
        foreach (Order o in multi.Take(3))
            Console.WriteLine($"  {o.Id,-12} {o.CustomerId,-8} сума: {o.Total,12:F2}");

        // Сценарій відмови: дубль id, неіснуючий id — людські повідомлення, без падіння
        Console.WriteLine();
        Console.WriteLine("--- Сценарії відмов ---");
        TryDo("дубль id", () => service.CreateOrder(newId, "CU-002"));
        TryDo("рядок у неіснуюче замовлення", () => service.AddLine("ORD-NOPE", "P-001", "Цемент", 185.50m, 1));
        TryDo("підтвердження неіснуючого", () => service.Confirm("ORD-NOPE"));
        TryDo("повторне підтвердження", () => service.Confirm(newId));

        return 0;
    }
    catch (JsonException)
    {
        Console.WriteLine($"Файл сховища пошкоджено: {Path.GetFullPath(StoreFactory.DataPath)}. Видаліть його і запустіть знову.");
        return 1;
    }
}

static void TryDo(string title, Action action)
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
