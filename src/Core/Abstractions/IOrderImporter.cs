using Core.Domain;

namespace Core.Abstractions;

// Асинхронний контракт імпорту замовлень.
public interface IOrderImporter
{
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);
}
