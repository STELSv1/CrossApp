namespace Core.Reports;

// Додаткове завдання 1: зведений DTO клієнта (результат GroupJoin клієнтів і замовлень).
public sealed record CustomerReport(string CustomerId, string Name, int Orders, decimal Revenue);
