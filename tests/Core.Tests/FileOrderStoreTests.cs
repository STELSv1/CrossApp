using Core.Domain;
using Core.Import;
using Core.Storage;

namespace Core.Tests;

// Тимчасовий каталог створюється для кожного тесту і прибирається у Dispose.
public sealed class FileOrderStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"crossapp-tests-{Guid.NewGuid():N}");
    private string FilePath => Path.Combine(_dir, "orders.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact] // дані переживають перезапуск: новий екземпляр бачить той самий запис
    public void Add_ThenNewInstance_ListsSameOrder()
    {
        var first = new FileOrderStore(FilePath);
        var order = Order.Create("ORD-PERSIST", "CU-1");
        order.AddLine("P-1", "Товар", 10m, 2);
        first.Add(order);

        var second = new FileOrderStore(FilePath);
        IReadOnlyList<Order> list = second.List();

        Assert.Single(list);
        Assert.Equal("ORD-PERSIST", list[0].Id);
        Assert.Equal(20m, list[0].Total);
    }

    [Fact] // атомарний запис: після збереження тимчасового файлу не лишається
    public void Flush_LeavesNoTempFile()
    {
        var store = new FileOrderStore(FilePath);
        store.Add(Order.Create("ORD-TMP", "CU-1"));

        Assert.True(File.Exists(FilePath));
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact] // експорт і імпорт зберігають всю колекцію, включно зі статусом і сумою
    public async Task Export_ThenImport_RoundTripsWholeCollection()
    {
        var order = Order.Create("ORD-RT", "CU-2");
        order.AddLine("P-2", "Інший товар", 5m, 3);
        order.Confirm();
        string exportPath = Path.Combine(_dir, "export.json");

        await JsonOrderExporter.ExportAsync([order], exportPath);
        IReadOnlyList<Order> imported = await new JsonOrderImporter().LoadAsync(exportPath);

        Assert.Single(imported);
        Assert.Equal(order.Total, imported[0].Total);
        Assert.Equal(OrderStatus.Confirmed, imported[0].Status);
    }
}
