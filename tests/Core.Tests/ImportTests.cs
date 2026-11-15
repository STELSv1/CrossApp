using Core.Domain;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

// Тести з тимчасовим файлом: диск використовується лише в межах тесту й прибирається у finally.
public class ImportTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"orders-test-{Guid.NewGuid():N}.json");

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

    [Fact] // успішний імпорт додає один раз; повторний імпорт нічого не дублює (ідемпотентність)
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
}
