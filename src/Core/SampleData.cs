using Core.Domain;
using Core.Dto;

namespace Core;

// Демонстраційні дані: 18 замовлень, 5 клієнтів, три статуси (Draft, Confirmed, Cancelled).
public static class SampleData
{
    private static readonly (string ProductId, string Name, decimal Price)[] Catalog =
    [
        ("P-001", "Цемент М400 25кг", 185.50m),
        ("P-002", "Пісок будівельний", 420.00m),
        ("P-003", "Цегла червона", 9.80m),
        ("P-004", "Фарба водоемульсійна 10л", 645.00m),
        ("P-005", "Шпаклівка фінішна", 58.00m),
    ];

    public static IEnumerable<Order> Orders()
    {
        for (int i = 1; i <= 18; i++)
        {
            Order order = Order.Create($"ORD-{i:000}", $"CU-{(i % 5) + 1:000}");
            int lineCount = i % 3 + 1;

            for (int k = 0; k < lineCount; k++)
            {
                var (productId, name, price) = Catalog[(i + k) % Catalog.Length];
                order.AddLine(productId, name, price, k + 1);
            }

            if (i % 2 == 0)
                order.Confirm();

            if (i % 5 == 0)
                order.Cancel();   // Draft→Cancelled або Confirmed→Cancelled — обидва дозволені

            yield return order;
        }
    }

    public static IEnumerable<CustomerDto> Customers() =>
    [
        new CustomerDto("CU-001", "Іван Петренко", "ivan@example.com"),
        new CustomerDto("CU-002", "Марія Коваль", null),
        new CustomerDto("CU-003", "Олег Бондар", "oleg@example.com"),
        new CustomerDto("CU-004", "Наталя Шевчук", null),
        new CustomerDto("CU-005", "Андрій Мельник", "andrii@example.com"),
    ];
}
