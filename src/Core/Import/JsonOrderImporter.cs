using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

// Справжній async-ввід-вивід: FileStream з useAsync і DeserializeAsync, без Task.Run.
public sealed class JsonOrderImporter : IOrderImporter
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
}
