using System.Reflection;
using System.Text.Json;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

// Збирання конфігурації. Порядок джерел (пізніші перекривають ранніші):
// appsettings.json → appsettings.{Env}.json → user-secrets → змінні CROSSAPP_* → аргументи --ключ=значення.
// Простішій змінній CROSSAPP_DATA_PATH віддається перевага над розділом Storage.
internal static class AppSettings
{
    private static readonly Dictionary<string, string> DataPathSwitches = new() // 2 додаткове
    {
        ["--data-path"] = "Storage:DataPath"
    };

    private static IConfiguration _configuration = new ConfigurationBuilder().Build();

    public static string EnvironmentName { get; private set; } = "Development";
    public static string DataFilePath { get; private set; } = string.Empty;
    public static string ExportFilePath { get; private set; } = string.Empty;

    // Секрет читається з конфігурації. Значення ніколи не друкується.
    public static string? ApiKey => _configuration["Integration:ApiKey"];

    // Повертає null, якщо конфігурація коректна; інакше — текст помилки для користувача.
    public static string? Load(string[] args)
    {
        EnvironmentName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        try
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true, reloadOnChange: false)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .AddEnvironmentVariables(prefix: "CROSSAPP_")
                .AddCommandLine(args.Where(a => a.StartsWith("--")).ToArray(), DataPathSwitches)
                .Build();
        }
        catch (Exception ex) when (ex is FileNotFoundException or JsonException or FormatException)
        {
            return $"не вдалося прочитати конфігурацію: {ex.Message}";
        }

        AppOptions bound = _configuration.Get<AppOptions>() ?? new AppOptions();
        bool fromCommandLine = args.Any(a => a.StartsWith("--data-path=", StringComparison.OrdinalIgnoreCase));
        string? legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");

        string dataPath = fromCommandLine ? bound.Storage.DataPath
            : !string.IsNullOrWhiteSpace(legacy) ? legacy
            : bound.Storage.DataPath;

        var storage = new StorageOptions
        {
            DataPath = dataPath,
            FileName = bound.Storage.FileName
        };

        if (string.IsNullOrWhiteSpace(storage.DataPath)) //додаткове 1
            return "DataPath порожній. Задайте Storage:DataPath у appsettings.json або --data-path=...";

        if (!storage.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return $"FileName '{storage.FileName}' має закінчуватися на .json.";

        DataFilePath = storage.ResolveFullPath(AppContext.BaseDirectory);
        ExportFilePath = Path.Combine(Path.GetDirectoryName(DataFilePath)!, "export-orders.json");
        return null;
    }

    public static int Print()
    {
        Console.WriteLine($"Середовище : {EnvironmentName}");
        Console.WriteLine($"Файл даних : {DataFilePath}");
        Console.WriteLine($"Експорт    : {ExportFilePath}");
        string? key = ApiKey;
        Console.WriteLine(key is null ? "ApiKey      : не заданий" : $"ApiKey      : присутній, довжина {key.Length}");
        return 0;
    }
}
