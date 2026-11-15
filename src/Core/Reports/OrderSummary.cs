namespace Core.Reports;

// DTO рядка звіту "дорогі замовлення".
public sealed record OrderSummary(string OrderId, string CustomerId, string Status, int Lines, decimal Total);
