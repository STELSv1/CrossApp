namespace Core.Events;

// Дані події: лише факти про зміну. Core нічого не знає про те, як їх покажуть чи запишуть.
public sealed class OrderChangedEventArgs(string orderId, OrderChangeKind kind, int linesCount, decimal total) : EventArgs
{
    public string OrderId { get; } = orderId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;
    public decimal Total { get; } = total;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
