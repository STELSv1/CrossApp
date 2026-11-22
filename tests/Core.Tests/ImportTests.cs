using Core.Domain;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

// Тести з тимчасовим файлом: диск використовується лише в межах тесту й прибирається у finally.
public class ImportTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"orders-test-{Guid.NewGuid():N}.json");

    // Синхронний приймач прогресу: зберігає значення для перевірки.
    private sealed class ListProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }

    [Fact] // зіпсований запис: доменний виняток з номером запису, нічого не додано
    public async Task ImportAsync_BadRecord_ThrowsDomainException_WithRecordNumber()
    {
        string path = TempPath();
        try
        {
            await File.WriteAllTextAsync(path, """
            [
              { "id": "ORD-1", "customerId": "CU-1", "status": "Draft", "lines": [] },
              { "id": "ORD-2", "customerId": "", "status": "Draft", "lines": [] }
            ]
            """);
            var service = new OrderService(new InMemoryOrderStore());

            DomainException ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.Empty(service.All());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // успішний імпорт додає один раз; повторний імпорт нічого не дублює
    public async Task ImportAsync_ValidFile_AddsOnce_ThenSkipsDuplicates()
    {
        string path = TempPath();
        try
        {
            await File.WriteAllTextAsync(path, """
            [ { "id": "ORD-1", "customerId": "CU-1", "status": "Draft", "lines": [] } ]
            """);
            var service = new OrderService(new InMemoryOrderStore());

            Assert.Equal(1, await service.ImportAsync(new JsonOrderImporter(), path));
            Assert.Equal(0, await service.ImportAsync(new JsonOrderImporter(), path));
            Assert.Single(service.All());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // прогрес: зростає і завершується 100
    public async Task ImportAsync_ReportsProgress_EndingAt100()
    {
        string path = TempPath();
        try
        {
            await File.WriteAllTextAsync(path, """
            [
              { "id": "ORD-P1", "customerId": "CU-1", "status": "Draft", "lines": [] },
              { "id": "ORD-P2", "customerId": "CU-1", "status": "Draft", "lines": [] }
            ]
            """);
            var progress = new ListProgress();
            var service = new OrderService(new InMemoryOrderStore());

            int added = await service.ImportAsync(new JsonOrderImporter(), path, default, progress);

            Assert.Equal(2, added);
            Assert.Equal(new[] { 50, 100 }, progress.Values);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // потоковий імпорт додає кожен запис
    public async Task ImportStreamAsync_AddsEachRecord()
    {
        string path = TempPath();
        try
        {
            await File.WriteAllTextAsync(path, """
            [
              { "id": "ORD-S1", "customerId": "CU-1", "status": "Draft", "lines": [] },
              { "id": "ORD-S2", "customerId": "CU-2", "status": "Draft", "lines": [] }
            ]
            """);
            var service = new OrderService(new InMemoryOrderStore());

            Assert.Equal(2, await service.ImportStreamAsync(new JsonOrderImporter(), path));
            Assert.Equal(2, service.All().Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // зв'язані токени: спрацював таймаут, а не користувач
    public async Task LinkedToken_TimeoutCancelled_UserTokenNotCancelled()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        await timeoutCts.CancelAsync();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", linked.Token));

        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.False(userCts.IsCancellationRequested);
    }
}
