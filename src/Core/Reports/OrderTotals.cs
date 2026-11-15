namespace Core.Reports;

// DTO підсумків: замість кортежу, щоб контракт звіту було видно в коді й легко перевірити тестом.
public sealed record OrderTotals(int Orders, decimal Revenue, decimal Average, decimal Max);
