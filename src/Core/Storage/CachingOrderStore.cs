using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Додаткове завдання 1: декоратор. Той самий контракт IOrderStore, нова поведінка —
// кеш читання поверх будь-якого іншого сховища, переданого в конструкторі.
// Сервіс не знає, що кеш існує: він отримує IOrderStore.
public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Order> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Order>? _all;

    public IReadOnlyList<Order> List() => _all ??= _inner.List();

    public Order? GetById(string id)
    {
        if (_byId.TryGetValue(id, out Order? cached))
            return cached;

        Order? found = _inner.GetById(id);
        if (found is not null)
            _byId[id] = found;
        return found;
    }

    public void Add(Order order)
    {
        _inner.Add(order);
        Remember(order);
    }

    public void Update(Order order)
    {
        _inner.Update(order);
        Remember(order);
    }

    public IReadOnlyList<Order> Where(Func<Order, bool> predicate) => _inner.Where(predicate);

    // Після будь-якої зміни скидаємо кеш списку, щоб не віддати застарілі дані.
    private void Remember(Order order)
    {
        _byId[order.Id] = order;
        _all = null;
    }
}
