using System.Globalization;
using Core.Dto;

namespace Core.Import;

// Додаткове завдання лаби 3: рядки різних типів за префіксом "P;" / "C;".
public static class MixedLineImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                case ["P", var id, var name, var price]
                    when !string.IsNullOrWhiteSpace(name)
                         && decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p)
                         && p >= 0:
                    products.Add(new ProductDto(id, name, p));
                    break;

                case ["P", _, _, _]:
                    errors.Add($"рядок {number}: некоректні дані товару (порожня назва або ціна)");
                    break;

                case ["C", var id, var name, var email] when !string.IsNullOrWhiteSpace(name):
                    customers.Add(new CustomerDto(id, name, string.IsNullOrWhiteSpace(email) ? null : email));
                    break;

                case ["C", _, _, _]:
                    errors.Add($"рядок {number}: некоректні дані клієнта (порожнє ім'я)");
                    break;

                case [var prefix, ..]:
                    errors.Add($"рядок {number}: невідомий префікс типу '{prefix}'");
                    break;

                default:
                    errors.Add($"рядок {number}: неможливо розібрати рядок");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }
}

public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CustomerDto> Customers,
    IReadOnlyList<string> Errors);
