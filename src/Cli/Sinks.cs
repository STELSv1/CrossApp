using Core.Abstractions;
using Core.Events;

namespace Cli;

// Підписник 1 (консоль): реалізація INotificationSink, яку знає тільки Cli.
internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs e) => Console.WriteLine(Format(e));

    internal static string Format(OrderChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-10} {e.OrderId} рядків: {e.LinesCount,2} сума: {e.Total,12:F2}";
}

// Підписник 2 (файл logs/app.log): той самий контракт, інша механіка.
internal sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public void Notify(OrderChangedEventArgs e) =>
        File.AppendAllText(_path, ConsoleSink.Format(e) + Environment.NewLine);
}
