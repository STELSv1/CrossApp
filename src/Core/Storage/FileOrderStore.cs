using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Файлова реалізація того самого контракту: кеш у пам'яті, дозавантаження при першому зверненні,
// запис на диск після кожної зміни. На диск ідуть DTO (формат тижня 3), не сутності.
public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    private readonly string _path = Path.GetFullPath(path);
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            List<OrderDto>? dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path));
            foreach (OrderDto dto in dtos ?? [])
            {
                Order order = Order.FromDto(dto); // відновлення через перевірки сутності
                _cache[order.Id] = order;
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        List<OrderDto> dtos = _cache.Values.Select(o => o.ToDto()).ToList();
        File.WriteAllText(_path, JsonSerializer.Serialize(dtos, Options));
    }

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureLoaded();

        if (_cache.ContainsKey(order.Id))
            throw new InvalidOperationException($"Замовлення з id={order.Id} уже існує.");

        _cache.Add(order.Id, order);
        Flush();
    }

    public void Update(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureLoaded();

        if (!_cache.ContainsKey(order.Id))
            throw new InvalidOperationException($"Немає замовлення з id={order.Id} для оновлення.");

        _cache[order.Id] = order;
        Flush();
    }

    public IReadOnlyList<Order> Where(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureLoaded();
        return _cache.Values.Where(predicate).ToList();
    }
}
