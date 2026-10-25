# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Замовлення**.
Сутності: `Customer` (клієнт), `Product` (товар), `Order` (замовлення), `OrderLine` (рядок замовлення).
Призначення: оформлення замовлень клієнтів і підрахунок сум.

## Структура solution

```
CrossApp/
  CrossApp.sln
  README.md
  .gitignore
  data/
    sample.csv / sample.json / mixed.csv   # тестові дані імпорту (лаба 3)
  src/
    Core/
      Core.csproj                # class library, multi-target: net8.0;net10.0
      EnvironmentInfo.cs         # лаби 1-2
      Dto/                       # ФОРМАТ даних (records, без правил)
        ProductDto.cs, CustomerDto.cs, ImportResult.cs
        OrderDto.cs, OrderLineDto.cs          # лаба 4
      Import/                    # лаба 3: CSV/JSON-імпортери
      Domain/                    # лаба 4: сутності з поведінкою та інваріантами
        Order.cs                 # агрегат: фабрика Create, AddLine, Confirm, Cancel, ToDto/FromDto
        OrderLine.cs             # рядок замовлення: незмінний, фабрика Create
        OrderStatus.cs           # Draft / Confirmed / Cancelled
    Cli/
      Cli.csproj                 # ProjectReference -> Core
      Program.cs                 # сценарії та вивід
```

Залежність одностороння: `Cli → Core`. `Core/Domain` не залежить ні від `Console`, ні від `File`, ні від `Cli`.

## Інваріанти доменної моделі (лаба 4)

| # | Правило | Виняток | Де перевіряється |
|---|---------|---------|------------------|
| 1 | Номер замовлення і ідентифікатор клієнта не порожні | `ArgumentException` | `Order.Create` |
| 2 | У рядку: Id товару і назва не порожні | `ArgumentException` | `OrderLine.Create` |
| 3 | Ціна в рядку не від'ємна | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| 4 | Кількість у рядку більша за нуль | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| 5 | Рядки додаються лише до замовлення зі статусом Draft | `InvalidOperationException` | `Order.AddLine` |
| 6 | Не можна підтвердити порожнє замовлення | `InvalidOperationException` | `Order.Confirm` |
| 7 | Допустимі лише переходи Draft→Confirmed, Draft→Cancelled, Confirmed→Cancelled | `InvalidOperationException` | `Order.Confirm` / `Order.Cancel` (через `CanMove`) |
| 8 | `FromDto` проходить ті самі перевірки (відновлення через `Create`/`AddLine`/`Confirm`) і відхиляє невідомий статус | `ArgumentException` / `InvalidOperationException` | `Order.FromDto` |

Стан захищено: властивості лише для читання (`private set` / без set), конструктори приватні,
колонка рядків назовні — `IReadOnlyList<OrderLine>`.

## Запуск

```
dotnet build
dotnet run --project src/Cli -- --domain       # лаба 4: сценарій успіху + порушення інваріантів
dotnet run --project src/Cli                    # лаба 3: імпорт data/sample.csv
dotnet run --project src/Cli -- data/sample.json
dotnet run --project src/Cli -- --mixed
dotnet run --project src/Cli -- --env           # лаби 1-2: інформація про середовище
```

## Формат файлу data/sample.csv

Роздільник `;`, колонки `id;name;price`, перший рядок — заголовок (необов'язковий),
кодування UTF-8, ціна парситься через `CultureInfo.InvariantCulture` (крапка).

## Публікація

| RID       | Режим               | Розмір publish | Потрібен встановлений runtime |
|-----------|---------------------|-----------------|--------------------------------|
| win-x64   | self-contained      | 76,86 МБ        | ні                              |
| win-x64   | framework-dependent | 0,19 МБ         | так (.NET 10)                   |

## Середовище

.NET SDK 10.0, `<ваша ОС і архітектура>`
