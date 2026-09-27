using System.Text.Json;
using Core.Dto;

namespace Core.Import;

// Додатковий імпортер (додаткове завдання 1): читає ті самі ProductDto з JSON-масиву.
// Так само, як і CSV-імпортер, НЕ перериває розбір через один пошкоджений запис —
// кожен елемент масиву обробляється незалежно, помилки збираються з номером запису.
public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        using JsonDocument document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            errors.Add("очікую JSON-масив об'єктів ProductDto");
            return new ImportResult<ProductDto>(items, errors);
        }

        int index = 0;
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            index++;
            try
            {
                ProductDto? dto = element.Deserialize<ProductDto>(Options);

                switch (dto)
                {
                    case null:
                        errors.Add($"запис {index}: порожній об'єкт");
                        break;
                    case { Id: null or "" } or { Name: null or "" }:
                        errors.Add($"запис {index}: відсутній обов'язковий Id або Name");
                        break;
                    case { Price: < 0 }:
                        errors.Add($"запис {index}: ціна не може бути від'ємною ({dto.Price})");
                        break;
                    default:
                        items.Add(dto);
                        break;
                }
            }
            catch (JsonException ex)
            {
                // напр. price заданий як текстовий рядок замість числа
                errors.Add($"запис {index}: {ex.Message}");
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}
