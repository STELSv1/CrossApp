using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

// Справжній async-ввід-вивід: FileStream з useAsync, без Task.Run.
// Реалізує два контракти: повне читання (LoadAsync) і потокове (StreamAsync).
public sealed class JsonOrderImporter : IOrderImporter, IOrderStreamImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();   // перевірка ДО відкриття файлу

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        List<OrderDto>? dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку замовлень.");

        var result = new List<Order>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                result.Add(Order.FromDto(dtos[i]));
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }
        }
        return result;
    }

    // Потоковий режим: кожен запис читається і перевіряється окремо, без списку всього файлу.
    public async IAsyncEnumerable<Order> StreamAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        int index = 0;
        await foreach (OrderDto? dto in JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, Options, ct))
        {
            index++;
            ct.ThrowIfCancellationRequested();

            if (dto is null)
                throw new DomainException($"Запис {index}: порожній об'єкт.");

            Order order;
            try
            {
                order = Order.FromDto(dto);
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Запис {index}: {ex.Message}", ex);
            }

            yield return order;
        }
    }
}
