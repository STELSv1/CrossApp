using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

// Експорт усієї колекції замовлень у JSON. Запис атомарний: спочатку .tmp, потім File.Move.
public static class JsonOrderExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    public static async Task ExportAsync(IEnumerable<Order> orders, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<OrderDto> dtos = orders.Select(o => o.ToDto()).ToList();

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tmp = path + ".tmp";
        await using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, dtos, Options, ct);
        }

        File.Move(tmp, path, overwrite: true);   // або старий файл цілий, або новий повністю записаний
    }
}
