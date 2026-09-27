using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з десятковою крапкою в ціні.
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            // патерн властивості — кількість колонок
            { Length: < 3 } => new ParseFailed($"очікую 3 колонки, отримав {parts.Length}"),

            // патерн списку з константою "" — порожня назва
            [_, "", _] => new ParseFailed("назва товару порожня"),

            // патерн списку + охоронна умова when — порожній Id
            [var id, _, _] when string.IsNullOrWhiteSpace(id)
                => new ParseFailed("Id порожній"),

            // патерн списку + when з TryParse — некоректна або від'ємна ціна
            [_, _, var price] when !decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) || p < 0
                => new ParseFailed($"ціна '{price}' не є коректним невід'ємним числом"),

            // успішний випадок — усі три поля коректні
            [var id, var name, var price]
                => new ParseOk(new ProductDto(id, name, decimal.Parse(price, NumberStyles.Number, CultureInfo.InvariantCulture))),

            // патерн-заглушка (discard) — забагато колонок
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
