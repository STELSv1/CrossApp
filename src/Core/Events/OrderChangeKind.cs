namespace Core.Events;

// Вид зміни стану замовлення. Подія сповіщає "що сталося", а не "що робити".
public enum OrderChangeKind
{
    Created,
    LineAdded,
    Confirmed
}
