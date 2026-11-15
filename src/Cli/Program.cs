using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Режими лаб 1–4 і 6
if (args.Contains("--env")) return LegacyModes.RunEnvironmentReport(args.Contains("--json"));
if (args.Contains("--domain")) return LegacyModes.RunDomainDemo();
if (args.Contains("--mixed")) return LegacyModes.RunMixedImport(args);
if (args.Contains("--import")) return LegacyModes.RunImport(args);
if (args.Contains("--events")) return EventsMode.Run(args);
if (args.Contains("--import-async")) return await AsyncImportMode.RunAsync(args);

// Лабораторна 7: підкоманди звітів (report-top, report-big, report-groups, ...)
string? reportCommand = args.FirstOrDefault(a => a.StartsWith("report-"));
if (reportCommand is not null)
    return RunReport(reportCommand, args);

// Лабораторна 5: сховища
return RunCatalogScenario(args);

// Program.cs лише викликає ReportService і друкує результат. Жодного Where/GroupBy тут немає.
static int RunReport(string command, string[] args)
{
    IOrderStore store = StoreFactory.Create(args);
    IReadOnlyList<Order> items = new OrderService(store).All();

    switch (command)
    {
        case "report-top":
        {
            Console.WriteLine("Топ-5 клієнтів за виручкою (без скасованих):");
            foreach (CustomerSummary c in ReportService.TopCustomers(items, 5))
                Console.WriteLine($"  {c.CustomerId,-8} замовлень: {c.OrdersCount,3}  сума: {c.Total,12:F2}");
            break;
        }
        case "report-big":
        {
            Console.WriteLine("Замовлення дорожчі за 1000:");
            foreach (OrderSummary o in ReportService.Expensive(items, 1000m))
                Console.WriteLine($"  {o.OrderId,-10} {o.CustomerId,-8} {o.Status,-10} рядків: {o.Lines,2}  сума: {o.Total,12:F2}");
            break;
        }
        case "report-groups":
        {
            Console.WriteLine("Групи за статусом:");
            PrintGroups(ReportService.ByStatus(items));
            break;
        }
        case "report-groups-q":
        {
            IReadOnlyList<GroupSummary> byMethod = ReportService.ByStatus(items);
            IReadOnlyList<GroupSummary> byQuery = ReportService.ByStatusQuery(items);
            Console.WriteLine("Групи за статусом (синтаксис запитів):");
            PrintGroups(byQuery);
            Console.WriteLine($"Збігається з методним синтаксисом: {byMethod.SequenceEqual(byQuery)}");
            break;
        }
        case "report-sum":
        {
            OrderTotals t = ReportService.Totals(items);
            Console.WriteLine($"  замовлень {t.Orders}, виручка {t.Revenue:F2}, середній чек {t.Average:F2}, макс {t.Max:F2}");
            break;
        }
        case "report-customers":
        {
            Console.WriteLine("Клієнти (GroupJoin з замовленнями):");
            foreach (CustomerReport r in ReportService.CustomerRevenue(items, SampleData.Customers()))
                Console.WriteLine($"  {r.CustomerId,-8} {r.Name,-18} замовлень: {r.Orders,3}  сума: {r.Revenue,12:F2}");
            break;
        }
        case "report-deferred":
        {
            var d = ReportService.DeferredDemo(items);
            Console.WriteLine("Відкладене виконання:");
            Console.WriteLine($"  до зміни джерела: {d.Before}");
            Console.WriteLine($"  після зміни джерела: {d.After}");
            Console.WriteLine($"  у знімку (ToList): {d.Snapshot}");
            Console.WriteLine($"  після очищення джерела запит знову дає: {d.AfterClear}");
            break;
        }
        case "report-bench":
        {
            var b = ReportBenchmark.Run(100_000);
            Console.WriteLine("Порівняння на 100 000 записах:");
            Console.WriteLine($"  з ToList посередині: {b.WithMidList.TotalMilliseconds,8:F1} мс");
            Console.WriteLine($"  без проміжного ToList: {b.WithoutMidList.TotalMilliseconds,8:F1} мс");
            Console.WriteLine($"  результати однакові: {b.SameResult}");
            break;
        }
        default:
            Console.WriteLine("report-top | report-big | report-groups | report-groups-q | report-sum | report-customers | report-deferred | report-bench");
            return 1;
    }

    return 0;
}

static void PrintGroups(IReadOnlyList<GroupSummary> groups)
{
    foreach (GroupSummary g in groups)
        Console.WriteLine($"  {g.Key,-10} замовлень: {g.Count,3}  сума: {g.Total,12:F2}");
}

// Лабораторна 5: сценарій сховищ (без змін логіки)
static int RunCatalogScenario(string[] args)
{
    IOrderStore store = StoreFactory.Create(args);
    var service = new OrderService(store);

    try
    {
        Console.WriteLine($"Сховище: {store.GetType().Name}");
        if (args.Contains("--file"))
            Console.WriteLine($"Файл: {Path.GetFullPath(StoreFactory.DataPath)}");

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

        IReadOnlyList<Order> multi = service.Search(o => o.IsConfirmed && o.Lines.Count > 1);
        Console.WriteLine($"Пошук (підтверджені з більш ніж одним рядком): знайдено {multi.Count}");

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
    catch (Exception ex) when (ex is ArgumentException or DomainException)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}
