using Core.Dto;

namespace Core.Domain;

// Рядок замовлення: незмінний після створення, створюється лише через Create.
public sealed class OrderLine
{
    public string ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public int Quantity { get; }
    public decimal Total => Price * Quantity;

    private OrderLine(string productId, string name, decimal price, int quantity)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    // Єдиний спосіб створити рядок: усі перевірки ДО створення об'єкта.
    public static OrderLine Create(string productId, string name, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару в рядку замовлення обов'язковий", nameof(productId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Назва товару {productId} не може бути порожньою", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price,
                $"Ціна товару {productId} не може бути від'ємною");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                $"Кількість товару {productId} у рядку має бути більшою за нуль");

        return new OrderLine(productId.Trim(), name.Trim(), price, quantity);
    }

    public OrderLineDto ToDto() => new(ProductId, Name, Price, Quantity);

    // FromDto іде через Create — ті самі перевірки, що й при звичайному створенні.
    public static OrderLine FromDto(OrderLineDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Create(dto.ProductId, dto.Name, dto.Price, dto.Quantity);
    }
}
