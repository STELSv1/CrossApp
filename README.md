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
    sample.csv / sample.json / mixed.csv     # тестові дані імпорту (лаба 3)
  src/
    Core/
      Core.csproj                  # class library, multi-target: net8.0;net10.0
      EnvironmentInfo.cs           # лаби 1-2
      SampleData.cs                # лаба 5: 15 демонстраційних замовлень
      Dto/                         # ФОРМАТ даних (records без правил)
      Import/                      # лаба 3: CSV/JSON-імпортери
      Domain/                      # лаба 4: Order, OrderLine, OrderStatus (інваріанти)
      Abstractions/
        IOrderStore.cs             # лаба 5: контракт сховища
      Storage/
        InMemoryOrderStore.cs      # лаба 5: реалізація в пам'яті
        FileOrderStore.cs          # лаба 5: реалізація у файлі (JSON, кеш, запис після змін)
      Services/
        OrderService.cs            # лаба 5: бізнес-операції, залежить лише від IOrderStore
    Cli/
      Cli.csproj                   # ProjectReference -> Core
      Program.cs                   # composition root (єдине місце з new ...Store)
      LegacyModes.cs               # режими лаб 1-4
```

Залежності: `Cli → Core`; у `Core` сервіс залежить від інтерфейсу `IOrderStore`, а не від класів сховищ.
`Core` не містить `Console.` і не посилається на `Microsoft.Extensions.DependencyInjection`.

## Сховище (лабораторна 5)

| Реалізація | Де зберігає | Як працює |
|---|---|---|
| `InMemoryOrderStore` | пам'ять | дані з `SampleData`, після виходу зникають |
| `FileOrderStore` | `data/catalog.json` поруч із застосунком | кеш у пам'яті, дозавантаження при першому зверненні, запис після кожної зміни |

Файл зберігає DTO (`OrderDto`), а не сутності. Відновлення через `Order.FromDto`, тож перевірки діють і під час читання з файлу.

## Запуск

```
dotnet build
dotnet run --project src/Cli -f net10.0                 # лаба 5: InMemoryOrderStore
dotnet run --project src/Cli -f net10.0 -- --file       # лаба 5: FileOrderStore (накопичує дані між запусками)
dotnet run --project src/Cli -f net10.0 -- --domain     # лаба 4: сценарії інваріантів
dotnet run --project src/Cli -f net10.0 -- --import     # лаба 3: імпорт data/sample.csv
dotnet run --project src/Cli -f net10.0 -- --mixed      # лаба 3: data/mixed.csv
dotnet run --project src/Cli -f net10.0 -- --env        # лаби 1-2: середовище
```

`-f net10.0` потрібен, бо `Cli` збирається під два фреймворки (`net8.0;net10.0`).

Файл `catalog.json` створюється в `src\Cli\bin\Debug\net10.0\data\` (каталог застосунку, `AppContext.BaseDirectory`).

## Інваріанти доменної моделі (лаба 4)

| # | Правило | Виняток | Де перевіряється |
|---|---------|---------|------------------|
| 1 | Номер замовлення і Id клієнта не порожні | `ArgumentException` | `Order.Create` |
| 2 | У рядку: Id товару і назва не порожні | `ArgumentException` | `OrderLine.Create` |
| 3 | Ціна в рядку не від'ємна | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| 4 | Кількість у рядку більша за нуль | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| 5 | Рядки додаються лише до замовлення у статусі Draft | `InvalidOperationException` | `Order.AddLine` |
| 6 | Не можна підтвердити порожнє замовлення | `InvalidOperationException` | `Order.Confirm` |
| 7 | Допустимі лише переходи Draft→Confirmed, Draft→Cancelled, Confirmed→Cancelled | `InvalidOperationException` | `Order.CanMove` |
| 8 | `FromDto` проходить ті самі перевірки і відхиляє невідомий статус | `ArgumentException` / `InvalidOperationException` | `Order.FromDto` |

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

## Події (лабораторна 6)

| Подія | Коли піднімається |
|---|---|
| `OrderService.Changed` (`EventHandler<OrderChangedEventArgs>`) | після успішного `CreateOrder`, `AddLine`, `Confirm` |

Підписники: `EventsMode.OnOrderChanged` (через `INotificationSink`: `ConsoleSink`, `FileSink`) і лямбда-лічильник.
Лог-файл: `src\Cli\bin\Debug\net10.0\logs\app.log`.

```
dotnet run --project src/Cli -f net10.0 -- --events
```

Делегати: `Func` — `OrderService.Search(predicate)`; `Action<string>` — `ProductCsvImporter.Load(path, onSkipped)`.
