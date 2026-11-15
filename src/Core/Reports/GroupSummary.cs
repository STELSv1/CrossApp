namespace Core.Reports;

// DTO групи: ключ групування, кількість елементів і сума.
public sealed record GroupSummary(string Key, int Count, decimal Total);
