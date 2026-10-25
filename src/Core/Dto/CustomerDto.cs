namespace Core.Dto;

// Клієнт. Email необов'язковий — не кожен клієнт лишає пошту.
public sealed record CustomerDto(
    string Id,
    string Name,
    string? Email = null);
