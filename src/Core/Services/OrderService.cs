using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Бізнес-операції над замовленнями. Сервіс не знає, де зберігаються дані,
// і не знає, хто слухає його події: він лише повідомляє, що зміна відбулася.
public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    // Подія піднімається ТІЛЬКИ після успішної зміни стану.
    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind) =>
        Changed?.Invoke(this, new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total));

    public Order CreateOrder(string id, string customerId)
    {
        Order order = Order.Create(id, customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.Created);
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = Require(orderId);
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string orderId)
    {
        Order order = Require(orderId);
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Confirmed);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) => _store.Where(predicate);

    // Лабораторна 8: імпорт з повним читанням. Додаткове завдання 3: прогрес через IProgress<int>.
    public async Task<int> ImportAsync(
        IOrderImporter importer,
        string path,
        CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        IReadOnlyList<Order> items = await importer.LoadAsync(path, ct);

        int added = 0;
        for (int i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            Order item = items[i];

            if (_store.GetById(item.Id) is null)     // ідемпотентність
            {
                _store.Add(item);
                OnChanged(item, OrderChangeKind.Created);
                added++;
            }

            progress?.Report((i + 1) * 100 / items.Count);
        }
        return added;
    }

    // Додаткове завдання 1: потоковий імпорт — кожне замовлення обробляється одразу після читання.
    public async Task<int> ImportStreamAsync(IOrderStreamImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        int added = 0;
        await foreach (Order item in importer.StreamAsync(path, ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(item.Id) is not null)
                continue;

            _store.Add(item);
            OnChanged(item, OrderChangeKind.Created);
            added++;
        }
        return added;
    }

    private Order Require(string id) =>
        _store.GetById(id) ?? throw new DomainException($"Немає замовлення з id={id}.");
}
