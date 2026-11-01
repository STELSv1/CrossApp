using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

// Бізнес-операції над замовленнями. Сервіс не знає, де зберігаються дані:
// він працює лише з IOrderStore, який передається ззовні через конструктор.
public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string id, string customerId)
    {
        Order order = Order.Create(id, customerId); // інваріанти перевіряє сутність
        _store.Add(order);                           // унікальність id — відповідальність сховища
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = Require(orderId);
        order.AddLine(productId, name, price, quantity); // правила сутності
        _store.Update(order);
    }

    public void Confirm(string orderId)
    {
        Order order = Require(orderId);
        order.Confirm();
        _store.Update(order);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    // Додаткове завдання 2: пошук за довільною умовою.
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) => _store.Where(predicate);

    private Order Require(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");
}
