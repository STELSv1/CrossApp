using Core;
using Core.Domain;
using Core.Dto;
using Core.Import;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// --domain -> лаба 4: сценарій успіху + сценарій порушення інваріантів
// --env    -> лаби 1-2: інформація про середовище
// --mixed  -> лаба 3 (додаткове): рядки за префіксом P;/C;
// без прапорців -> лаба 3: імпорт CSV/JSON
if (args.Contains("--domain"))
    return RunDomainDemo();

if (args.Contains("--env"))
    return RunEnvironmentReport(args.Contains("--json"));

if (args.Contains("--mixed"))
    return RunMixedImport(args);

return RunImport(args);

// ===================== Лабораторна 4: доменна модель =====================

static int RunDomainDemo()
{
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Order order = Order.Create("ORD-001", "CU-001");
    order.AddLine("O-001", "Ноутбук ASUS VivoBook 15", 24999.99m, 1);
    order.AddLine("O-002", "Миша бездротова Logitech M185", 349.50m, 2);
    Console.WriteLine(order);

    order.Confirm();
    Console.WriteLine(order);
    Console.WriteLine();

    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
    Order draft = Order.Create("ORD-002", "CU-002");
    Order cancelled = Order.Create("ORD-003", "CU-003");
    cancelled.Cancel();

    TryDo("рядок у підтверджене замовлення", () => order.AddLine("O-003", "Клавіатура", 1899m, 1));
    TryDo("порожній клієнт", () => Order.Create("ORD-004", " "));
    TryDo("нульова кількість у рядку", () => draft.AddLine("O-004", "Кабель HDMI", 149.90m, 0));
    TryDo("від'ємна ціна", () => draft.AddLine("O-005", "Флешка", -10m, 1));
    TryDo("підтвердження порожнього замовлення", () => draft.Confirm());
    TryDo("підтвердження скасованого замовлення", () => cancelled.Confirm());
    Console.WriteLine();

    Console.WriteLine("Стан після усіх відмов (має лишитись незмінним):");
    Console.WriteLine($"  {order}");
    Console.WriteLine($"  {draft}");
    Console.WriteLine();

    Console.WriteLine("=== Мапінг ToDto / FromDto ===");
    OrderDto dto = order.ToDto();
    Order restored = Order.FromDto(dto);
    Console.WriteLine($"  відновлено з DTO: {restored}");

    OrderDto badLine = dto with { Lines = [new OrderLineDto("O-009", "Брак", 10m, -3)] };
    OrderDto badStatus = dto with { Status = "Shipped" };
    OrderDto emptyConfirmed = dto with { Lines = [] };

    TryDo("FromDto: від'ємна кількість", () => Order.FromDto(badLine));
    TryDo("FromDto: невідомий статус", () => Order.FromDto(badStatus));
    TryDo("FromDto: підтверджене без рядків", () => Order.FromDto(emptyConfirmed));
    Console.WriteLine();

    // Додаткове завдання 1: сутності + перелік DTO, що не пройшли інваріанти.
    Console.WriteLine("=== Додаткове: FromDtos (сутності + помилки) ===");
    ImportResult<Order> batch = Order.FromDtos([dto, badLine, badStatus, emptyConfirmed]);
    Console.WriteLine($"  Прийнято: {batch.Items.Count}, відхилено: {batch.Errors.Count}");
    foreach (string e in batch.Errors)
        Console.WriteLine($"  ! {e.ReplaceLineEndings(" ")}");

    return 0;
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message.ReplaceLineEndings(" ")}");
    }
}

// ===================== Лабораторна 3: імпорт =====================

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

static void PrintStats(int accepted, int rejected)
{
    int total = accepted + rejected;
    double errorPercent = total == 0 ? 0 : (double)rejected / total * 100;
    Console.WriteLine($"Усього: {total}, прийнято: {accepted}, пропущено: {rejected} ({errorPercent:F1}% помилок)");
}

// ===================== Лаби 1-2: середовище =====================

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
