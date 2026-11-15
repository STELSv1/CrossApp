using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

// Додаткове завдання 4: порівняння часу з ToList у середині ланцюжка і без нього.
public static class ReportBenchmark
{
    public static (TimeSpan WithMidList, TimeSpan WithoutMidList, bool SameResult) Run(int count)
    {
        List<Order> orders = Generate(count);

        var sw = Stopwatch.StartNew();
        List<string> withMid = orders
            .Where(o => o.Total > 0m)
            .ToList()                                   // проміжна матеріалізація
            .OrderByDescending(o => o.Total)
            .Select(o => o.Id)
            .ToList();
        TimeSpan t1 = sw.Elapsed;

        sw.Restart();
        List<string> withoutMid = orders
            .Where(o => o.Total > 0m)
            .OrderByDescending(o => o.Total)
            .Select(o => o.Id)
            .ToList();
        TimeSpan t2 = sw.Elapsed;

        return (t1, t2, withMid.SequenceEqual(withoutMid));
    }

    private static List<Order> Generate(int count)
    {
        var list = new List<Order>(count);
        for (int i = 0; i < count; i++)
        {
            Order o = Order.Create($"ORD-{i:000000}", $"CU-{i % 100:000}");
            o.AddLine("P-001", "Товар", (i % 1000) + 1m, 1);
            list.Add(o);
        }
        return list;
    }
}
