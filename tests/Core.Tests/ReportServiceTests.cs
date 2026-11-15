using Core.Domain;
using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory] // звіт на кількох наборах вхідних даних
    [InlineData(1000, 2)]
    [InlineData(10000, 0)]
    [InlineData(0, 4)]
    public void Expensive_CountsOrdersAboveThreshold(decimal threshold, int expected) =>
        Assert.Equal(expected, ReportService.Expensive(TestData.Orders(), threshold).Count);

    [Fact] // детермінований топ-N: скасовані не враховуються, CU-C (5000, скасовано) виключено
    public void TopCustomers_ExcludesCancelled_AndIsDeterministic() =>
        Assert.Equal(new[] { "CU-B", "CU-A" },
            ReportService.TopCustomers(TestData.Orders(), 2).Select(c => c.CustomerId));

    [Fact] // порожнє джерело не дає винятку
    public void Totals_OnEmptySource_ReturnsZeros()
    {
        OrderTotals t = ReportService.Totals(Array.Empty<Order>());
        Assert.Equal(0, t.Orders);
        Assert.Equal(0m, t.Average);
        Assert.Equal(0m, t.Max);
    }

    [Fact] // групування покриває всі замовлення
    public void ByStatus_CoversAllOrders()
    {
        var groups = ReportService.ByStatus(TestData.Orders());
        Assert.Equal(4, groups.Sum(g => g.Count));
        Assert.Equal(2, groups.Single(g => g.Key == "Confirmed").Count);
    }
}
