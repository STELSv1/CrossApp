using Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool jsonMode = args.Contains("--json");

EnvironmentReport report = EnvironmentInfo.Collect();

if (jsonMode)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };
    Console.WriteLine(JsonSerializer.Serialize(report, options));
}
else
{
    PrintTable(report);
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

// Розбиває довгий рядок на кілька рядків не ширше maxWidth символів,
// щоб клітинка таблиці не ламала рамку у вузькому терміналі.
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
