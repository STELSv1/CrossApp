namespace Core.Dto;

// Товар у замовленні. Id і Name — завжди мають бути (без них рядок не має сенсу),
// Note — необов'язковий коментар до позиції, тому позначений як string?.
public record ProductDto(
    string Id,
    string Name,
    decimal Price,
    string? Note = null);
