using Core.Domain;

namespace Core.Abstractions;

// Додаткове завдання 1: потоковий контракт. Замовлення віддаються по одному, без буфера всього файлу.
public interface IOrderStreamImporter
{
    IAsyncEnumerable<Order> StreamAsync(string path, CancellationToken ct = default);
}
