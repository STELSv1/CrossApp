namespace Core.Dto;

// Клієнт, що оформлює замовлення. Email необов'язковий — не кожен клієнт
// лишає контактну пошту, тому це единe nullable-поле в типі.
public sealed record CustomerDto(
    string Id,
    string Name,
    string? Email = null);
