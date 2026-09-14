using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool jsonMode = args.Contains("--json");

var info = new EnvironmentInfo(
    Title: "CrossApp – практикум з крос-платформного програмування",
    Student: "Моргулець Валентин, група ФЕІ-37(1)",
    OsDescription: RuntimeInformation.OSDescription,
    OsEnvironment: Environment.OSVersion.ToString(),
    ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion: Environment.Version.ToString(),
    Runtime: RuntimeInformation.FrameworkDescription,
    AppDirectory: AppContext.BaseDirectory,
    CurrentDirectory: Environment.CurrentDirectory,
    Domain: "Замовлення (клієнти, товари, замовлення, рядки замовлення)"
);

if (jsonMode)
{
    // За замовчуванням System.Text.Json екранує кирилицю як \uXXXX.
    // Дозволяємо кирилицю (та розширену латиницю) виводитись як звичайний текст.
    var options = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    PrintTable(info);
}

static void PrintTable(EnvironmentInfo info)
{
    const int maxValueWidth = 50; // фіксована ширина колонки значень — таблиця не "розповзається" в консолі

    (string Label, string Value)[] rows =
    [
        ("Заголовок", info.Title),
        ("Студент", info.Student),
        ("ОС (OSDescription)", info.OsDescription),
        ("ОС (Environment)", info.OsEnvironment),
        ("Архітектура процесу", info.ProcessArchitecture),
        ("Версія .NET (CLR)", info.DotNetVersion),
        ("Runtime", info.Runtime),
        ("Каталог застосунку", info.AppDirectory),
        ("Поточний каталог", info.CurrentDirectory),
        ("Предметна область", info.Domain),
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

record EnvironmentInfo(
    string Title,
    string Student,
    string OsDescription,
    string OsEnvironment,
    string ProcessArchitecture,
    string DotNetVersion,
    string Runtime,
    string AppDirectory,
    string CurrentDirectory,
    string Domain
);
