using Core.Domain;

namespace Core;

// Демонстраційні дані: 15 замовлень (парні підтверджені, непарні чернетки) для демо і тижня 7.
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
        for (int i = 1; i <= 15; i++)
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

            yield return order;
        }
    }
}
