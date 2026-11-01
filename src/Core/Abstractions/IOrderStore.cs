using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища замовлень. Сервіс знає лише цей інтерфейс, а не конкретний клас.
// Where додано для пошуку з предикатом (додаткове завдання 2).
public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    void Add(Order order);
    void Update(Order order);
    IReadOnlyList<Order> Where(Func<Order, bool> predicate);
}
