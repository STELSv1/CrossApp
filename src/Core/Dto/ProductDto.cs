namespace Core.Dto;

// Товар у замовленні. Id, Name — завжди мають бути; Note — необов'язковий (string?).
public record ProductDto(
    string Id,
    string Name,
    decimal Price,
    string? Note = null);
