namespace Core.Configuration;

// Налаштування застосунку. Core не знає, звідки вони взяті (файл, змінні середовища, аргументи).
public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders-store.json";

    // Відносний DataPath рахується від базового каталогу застосунку, а не від поточного каталогу терміналу.
    public string ResolveFullPath(string baseDir)
    {
        string root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}
