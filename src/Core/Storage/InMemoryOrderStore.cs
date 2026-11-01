using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Реалізація в пам'яті: Dictionary + початкові дані через конструктор. Після завершення програми дані зникають.
public sealed class InMemoryOrderStore(IEnumerable<Order>? seed = null) : IOrderStore
{
    private readonly Dictionary<string, Order> _items =
        (seed ?? []).ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List() => _items.Values.ToList();

    public Order? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (_items.ContainsKey(order.Id))
            throw new InvalidOperationException($"Замовлення з id={order.Id} уже існує.");
        _items.Add(order.Id, order);
    }

    public void Update(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!_items.ContainsKey(order.Id))
            throw new InvalidOperationException($"Немає замовлення з id={order.Id} для оновлення.");
        _items[order.Id] = order;
    }

    public IReadOnlyList<Order> Where(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _items.Values.Where(predicate).ToList();
    }
}
