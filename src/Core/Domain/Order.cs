using System.Globalization;
using Core.Dto;

namespace Core.Domain;

// Замовлення — агрегат: володіє своїми рядками і захищає правила.
// Domain не залежить від Console, File чи Cli.
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; }

    // Назовні — лише для читання: додати/видалити рядок в обхід AddLine неможливо.
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public bool IsConfirmed => Status == OrderStatus.Confirmed;
    public decimal Total => _lines.Sum(l => l.Total);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }

    // Єдиний спосіб створити замовлення: усі перевірки тут.
    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Номер замовлення обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException($"Замовлення {id}: ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        // 1) стан: рядки можна додавати лише до чернетки
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                $"Замовлення {Id} має статус {Status}, рядки додавати не можна");

        // 2) аргументи: усі перевірки всередині OrderLine.Create — ДО зміни стану
        OrderLine line = OrderLine.Create(productId, name, price, quantity);

        // 3) єдине місце зміни стану
        _lines.Add(line);
    }

    public void Confirm()
    {
        EnsureCanMoveTo(OrderStatus.Confirmed);

        if (_lines.Count == 0)
            throw new InvalidOperationException(
                $"Замовлення {Id} порожнє, підтвердити його неможливо");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        EnsureCanMoveTo(OrderStatus.Cancelled);
        Status = OrderStatus.Cancelled;
    }

    // Допустимі переходи між станами — switch expression (додаткове завдання 3).
    private static bool CanMove(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Draft, OrderStatus.Confirmed) => true,
        (OrderStatus.Draft, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        _ => false
    };

    private void EnsureCanMoveTo(OrderStatus target)
    {
        if (!CanMove(Status, target))
            throw new InvalidOperationException(
                $"Замовлення {Id}: перехід зі стану {Status} у {target} неможливий");
    }

    // ---- Мапінг у формат тижня 3 і назад (знадобиться сховищу тижня 5) ----

    public OrderDto ToDto() =>
        new(Id, CustomerId, Status.ToString(), _lines.Select(l => l.ToDto()).ToList());

    // FromDto відновлює сутність через Create / AddLine / Confirm / Cancel —
    // тобто проходить ТІ САМІ перевірки, що й звичайна робота з об'єктом.
    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Order order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);

        if (!Enum.TryParse(dto.Status, ignoreCase: true, out OrderStatus status) || !Enum.IsDefined(status))
            throw new ArgumentException(
                $"Замовлення {dto.Id}: невідомий статус '{dto.Status}'", nameof(dto));

        switch (status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        return order;
    }

    // Додаткове завдання 1: та сама ідея "дані + помилки", що й в імпорті тижня 3.
    // Коректні DTO стають сутностями, некоректні потрапляють у перелік помилок.
    public static ImportResult<Order> FromDtos(IEnumerable<OrderDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        var items = new List<Order>();
        var errors = new List<string>();
        int index = 0;

        foreach (OrderDto dto in dtos)
        {
            index++;
            try
            {
                items.Add(FromDto(dto));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                errors.Add($"запис {index} ({dto?.Id}): {ex.Message}");
            }
        }

        return new ImportResult<Order>(items, errors);
    }

    public override string ToString() =>
        $"{Id} (клієнт {CustomerId}) [{Status}] рядків: {_lines.Count}, сума: {Total.ToString("F2", CultureInfo.InvariantCulture)}";
}
