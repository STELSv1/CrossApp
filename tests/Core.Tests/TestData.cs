using Core.Domain;

namespace Core.Tests;

// Фіксований маленький набір для тестів (не SampleData, щоб тести не залежали від демо-даних).
// Суми: T-1 = 100 (Draft), T-2 = 2000 (Confirmed), T-3 = 5000 (Cancelled), T-4 = 300 (Confirmed).
internal static class TestData
{
    public static List<Order> Orders()
    {
        var o1 = Order.Create("T-1", "CU-A");
        o1.AddLine("P-1", "Товар", 100m, 1);

        var o2 = Order.Create("T-2", "CU-B");
        o2.AddLine("P-2", "Товар", 1000m, 2);
        o2.Confirm();

        var o3 = Order.Create("T-3", "CU-C");
        o3.AddLine("P-3", "Товар", 5000m, 1);
        o3.Cancel();

        var o4 = Order.Create("T-4", "CU-A");
        o4.AddLine("P-4", "Товар", 300m, 1);
        o4.Confirm();

        return [o1, o2, o3, o4];
    }
}
