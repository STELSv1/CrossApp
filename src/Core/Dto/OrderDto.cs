namespace Core.Dto;

// ФОРМАТ даних замовлення. Status — рядок ("Draft"/"Confirmed"/"Cancelled"),
// щоб файл лишався читабельним. Коректність перевіряє сутність Order.FromDto.
public sealed record OrderDto(
    string Id,
    string CustomerId,
    string Status,
    IReadOnlyList<OrderLineDto> Lines);
