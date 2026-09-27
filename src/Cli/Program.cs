using Core;
using Core.Dto;
using Core.Import;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// --env повертає стару поведінку з лаб 1-2 (інформація про середовище),
// за замовчуванням Cli тепер демонструє імпорт CSV (лабораторна 3).
if (args.Contains("--env"))
{
    return RunEnvironmentReport(args.Contains("--json"));
}

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

    ImportResult<ProductDto> result = ProductCsvImporter.Load(path);

    if (jsonMode)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };
        Console.WriteLine(JsonSerializer.Serialize(result, options));
        return 0;
    }

    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Name,-30} {p.Price,10:F2}");

    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }

    return 0;
}

static int RunEnvironmentReport(bool jsonMode)
{
    EnvironmentReport report = EnvironmentInfo.Collect();

    if (jsonMode)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };
        Console.WriteLine(JsonSerializer.Serialize(report, options));
        return 0;
    }

    PrintTable(report);
    return 0;
}

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
