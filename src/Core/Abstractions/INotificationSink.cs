using Core.Events;

namespace Core.Abstractions;

// Додаткове завдання 1: отримувач сповіщень. Реалізації (консоль, файл) живуть у Cli.
public interface INotificationSink
{
    void Notify(OrderChangedEventArgs e);
}
