namespace Core.Reports;

// DTO рядка звіту "топ клієнтів": контракт звіту, а не внутрішня структура Order.
public sealed record CustomerSummary(string CustomerId, int OrdersCount, decimal Total);
