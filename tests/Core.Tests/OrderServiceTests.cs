using Core;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact] // інваріант: відмова і незмінний стан
    public void AddLine_WithZeroQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        service.CreateOrder("ORD-T", "CU-1");
        service.AddLine("ORD-T", "P-1", "Товар", 10m, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine("ORD-T", "P-2", "Інший", 10m, 0));
        Assert.Single(store.GetById("ORD-T")!.Lines);
    }

    [Fact] // доменний виняток для бізнес-правила
    public void Confirm_EmptyOrder_ThrowsDomainException()
    {
        var service = new OrderService(new InMemoryOrderStore());
        service.CreateOrder("ORD-E", "CU-1");
        Assert.Throws<DomainException>(() => service.Confirm("ORD-E"));
    }

    [Fact] // подія піднялась рівно один раз
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.CreateOrder("ORD-EV", "CU-1");

        Assert.Single(events);
        Assert.Equal(OrderChangeKind.Created, events[0].Kind);
    }

    [Fact] // скасування: токен скасовано до початку роботи
    public async Task ImportAsync_WhenAlreadyCancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", cts.Token));
    }
}
