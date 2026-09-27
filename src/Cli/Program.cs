using Core;
using Core.Dto;
using Core.Import;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// --env   -> стара поведінка з лаб 1-2 (інформація про середовище)
// --mixed -> додаткове завдання 2: розпізнавання рядків за префіксом типу (data/mixed.csv)
// за замовчуванням -> імпорт CSV/JSON (лабораторна 3 + додаткове завдання 1)
if (args.Contains("--env"))
    return RunEnvironmentReport(args.Contains("--json"));

if (args.Contains("--mixed"))
    return RunMixedImport(args);

return RunImport(args);

static int RunImport(string[] args)
{
    string? explicitPath = args.FirstOrDefault(a => !a.StartsWith("--"));
    string path = explicitPath ?? Path.Combine("data", "sample.csv");
    bool jsonMode = args.Contains("--json");

    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    // Додаткове завдання 1: імпортер обирається за розширенням файлу через switch expression.
    string extension = Path.GetExtension(path).ToLowerInvariant();
    ImportResult<ProductDto>? result = extension switch
    {
        ".csv" => ProductCsvImporter.Load(path),
        ".json" => ProductJsonImporter.Load(path),
        _ => null
    };

    if (result is null)
    {
        Console.WriteLine($"Непідтримуване розширення файлу: '{extension}' (очікую .csv або .json)");
        return 1;
    }

    if (jsonMode)
    {
        Console.WriteLine(JsonSerializer.Serialize(result, BuildJsonOptions()));
        return 0;
    }

    Console.WriteLine($"Джерело: {path} ({extension})");
    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Name,-30} {p.Price,10:F2}");

    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }

    // Додаткове завдання 3: статистика імпорту одним рядком.
    PrintStats(result.Items.Count, result.Errors.Count);

    return 0;
}

static int RunMixedImport(string[] args)
{
    string? explicitPath = args.FirstOrDefault(a => !a.StartsWith("--"));
    string path = explicitPath ?? Path.Combine("data", "mixed.csv");

    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    MixedImportResult result = MixedLineImporter.Load(path);

    Console.WriteLine($"Джерело: {path} (мішаний формат P;/C;)");
    Console.WriteLine($"Товарів: {result.Products.Count}");
    foreach (ProductDto p in result.Products)
        Console.WriteLine($"  [P] {p.Id,-6} {p.Name,-28} {p.Price,10:F2}");

    Console.WriteLine($"Клієнтів: {result.Customers.Count}");
    foreach (CustomerDto c in result.Customers)
        Console.WriteLine($"  [C] {c.Id,-6} {c.Name,-24} {c.Email ?? "(немає email)"}");

    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }

    PrintStats(result.Products.Count + result.Customers.Count, result.Errors.Count);

    return 0;
}

// Додаткове завдання 3: усього / прийнято / пропущено / % помилок одним рядком.
static void PrintStats(int accepted, int rejected)
{
    int total = accepted + rejected;
    double errorPercent = total == 0 ? 0 : (double)rejected / total * 100;
    Console.WriteLine($"Усього: {total}, прийнято: {accepted}, пропущено: {rejected} ({errorPercent:F1}% помилок)");
}

static int RunEnvironmentReport(bool jsonMode)
{
    EnvironmentReport report = EnvironmentInfo.Collect();

    if (jsonMode)
    {
        Console.WriteLine(JsonSerializer.Serialize(report, BuildJsonOptions()));
        return 0;
    }

    PrintTable(report);
    return 0;
}

static JsonSerializerOptions BuildJsonOptions() => new()
{
    WriteIndented = false,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
};

static void PrintTable(EnvironmentReport report)
{
    const int maxValueWidth = 50;

    (string Label, string Value)[] rows =
    [
        ("Заголовок", report.Title),
        ("Студент", report.Student),
        ("Предметна область", report.Domain),
        ("ОС (OSDescription)", report.OsDescription),
        ("Runtime", report.FrameworkDescription),
        ("Архітектура процесу", report.ProcessArchitecture),
        ("RID (визначено вручну)", report.DetectedRid),
        ("RID (від .NET)", report.ReportedRid),
        ("Каталог застосунку", report.BaseDirectory),
        ("Примітка збірки", report.BuildNote),
    ];

    int labelWidth = rows.Max(r => r.Label.Length);
    int valueWidth = Math.Min(maxValueWidth, rows.Max(r => r.Value.Length));

    string border = $"+{new string('-', labelWidth + 2)}+{new string('-', valueWidth + 2)}+";

    Console.WriteLine(border);
    foreach (var (label, value) in rows)
    {
        var lines = WrapText(value, valueWidth);
        for (int i = 0; i < lines.Count; i++)
        {
            string labelCell = i == 0 ? label : string.Empty;
            Console.WriteLine($"| {labelCell.PadRight(labelWidth)} | {lines[i].PadRight(valueWidth)} |");
        }
        Console.WriteLine(border);
    }
}

static List<string> WrapText(string text, int maxWidth)
{
    var result = new List<string>();
    int start = 0;
    while (start < text.Length)
    {
        int len = Math.Min(maxWidth, text.Length - start);
        result.Add(text.Substring(start, len));
        start += len;
    }
    return result.Count == 0 ? [""] : result;
}
