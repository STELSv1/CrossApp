using Core.Domain;
using Core.Dto;

namespace Core.Reports;

// Усі аналітичні запити. Приймають IEnumerable<Order> і повертають DTO.
// Жодного Console у Core: вивід робить Cli.
public static class ReportService
{
    // 1) Фільтр + групування + агрегат + проєкція + топ-N з другим ключем.
    //    Відповідає на питання: які клієнти принесли найбільшу виручку?
    public static IReadOnlyList<CustomerSummary> TopCustomers(IEnumerable<Order> source, int n)
    {
        IReadOnlyList<CustomerSummary> all = source
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .ToList();

        return all.TopBy(c => c.Total, c => c.CustomerId, n).ToList();
    }

    // 2) Фільтр за умовою + сортування з другим ключем + проєкція.
    //    Відповідає на питання: які замовлення дорожчі за поріг?
    public static IReadOnlyList<OrderSummary> Expensive(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
              .OrderByDescending(o => o.Total)
              .ThenBy(o => o.Id, StringComparer.Ordinal)
              .Select(o => new OrderSummary(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total))
              .ToList();

    // 3) Групування з підрахунком і сумою (методний синтаксис).
    //    Відповідає на питання: як розподілені замовлення за статусами?
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status)
              .Select(g => new GroupSummary(g.Key.ToString(), g.Count(), g.Sum(o => o.Total)))
              .OrderBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 3б) Те саме групування, але в синтаксисі запитів (додаткове завдання 3).
    public static IReadOnlyList<GroupSummary> ByStatusQuery(IEnumerable<Order> source) =>
        (from o in source
         group o by o.Status into g
         select new GroupSummary(g.Key.ToString(), g.Count(), g.Sum(o => o.Total)))
        .OrderBy(s => s.Key, StringComparer.Ordinal)
        .ToList();

    // 4) Агрегати з обробкою порожньої послідовності (Average і Max на порожньому дають виняток).
    public static OrderTotals Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        if (list.Count == 0)
            return new OrderTotals(0, 0m, 0m, 0m);

        return new OrderTotals(
            list.Count,
            list.Sum(o => o.Total),
            list.Average(o => o.Total),
            list.Max(o => o.Total));
    }

    // Додаткове завдання 1: Join/GroupJoin клієнтів із замовленнями у зведений DTO.
    //    Клієнт без замовлень теж потрапляє у звіт (з нулями), бо це зовнішній зв'язок.
    public static IReadOnlyList<CustomerReport> CustomerRevenue(IEnumerable<Order> orders, IEnumerable<CustomerDto> customers) =>
        customers.GroupJoin(
                orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new CustomerReport(c.Id, c.Name, os.Count(), os.Sum(o => o.Total)))
            .OrderBy(r => r.CustomerId, StringComparer.Ordinal)
            .ToList();

    // Відкладене виконання: запит будується, але не виконується до перебирання.
    // Повертає: скільки до змін, після зміни джерела, у знімку, і після очищення джерела.
    public static (int Before, int After, int Snapshot, int AfterClear) DeferredDemo(IEnumerable<Order> items)
    {
        var source = items.ToList();
        var query = source.Where(o => o.Total > 500m);   // побудовано, не виконано

        int before = query.Count();                      // виконання №1

        var tmp = Order.Create("ORD-TMP01", "CU-DEMO");
        tmp.AddLine("P-DEMO", "Демонстраційна позиція", 1000m, 1);
        source.Add(tmp);

        int after = query.Count();                       // виконання №2 бачить нову зміну

        var snapshot = query.ToList();                   // результат зафіксовано у списку
        source.Clear();

        int afterClear = query.Count();                  // запит знову виконався на порожньому джерелі
        return (before, after, snapshot.Count, afterClear);
    }
}
