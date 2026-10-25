namespace Core.Dto;

// ФОРМАТ даних рядка замовлення (для файлів/сховища). Жодних перевірок — це лише носій даних.
public sealed record OrderLineDto(
    string ProductId,
    string Name,
    decimal Price,
    int Quantity);
