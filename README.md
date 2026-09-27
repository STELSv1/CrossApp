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
    sample.csv             # тестові дані CSV (11 коректних + 3 пошкоджені рядки)
    sample.json            # ті самі дані у JSON (4 коректні + 2 пошкоджені записи)
    mixed.csv               # додаткове завдання 2: змішані рядки P;/C; (товари й клієнти)
  src/
    Core/
      Core.csproj          # class library, multi-target: net8.0;net10.0
      EnvironmentInfo.cs    # лаби 1-2: EnvironmentReport + Collect()
      Dto/
        ProductDto.cs        # record: Id, Name, Price, Note?
        CustomerDto.cs       # record: Id, Name, Email?
        ImportResult.cs      # ImportResult<T>(Items, Errors)
      Import/
        ProductCsvImporter.cs  # розбір CSV через switch expression з патернами
        ProductJsonImporter.cs # додаткове завдання 1: той самий ProductDto з JSON
        MixedLineImporter.cs   # додаткове завдання 2: рядки за префіксом P;/C;
    Cli/
      Cli.csproj             # ProjectReference -> Core
      Program.cs               # аргументи, виклик імпорту, вивід
```

Залежність одностороння: `Cli → Core`. `Core` нічого не знає про `Cli`.

## Формат файлу data/sample.csv

- Роздільник колонок: `;` (крапка з комою) — не конфліктує з десятковою крапкою в ціні.
- Колонки: `id;name;price`.
- Перший рядок — заголовок (`id;name;price`), розпізнається і пропускається автоматично;
  файл без заголовка так само коректно імпортується (перший рядок просто буде розібраний
  як звичайні дані).
- Кодування: UTF-8 (читається явно через `File.ReadAllLines(path, Encoding.UTF8)`).
- Ціна парситься з `CultureInfo.InvariantCulture`, роздільник дробової частини — крапка
  (`24999.99`, не `24999,99`).

## Запуск

```
dotnet build
dotnet run --project src/Cli                        # імпорт data/sample.csv (за замовчуванням)
dotnet run --project src/Cli -- data/sample.csv       # імпорт конкретного CSV-файлу
dotnet run --project src/Cli -- data/sample.json       # той самий імпорт, але з JSON (додаткове завдання 1)
dotnet run --project src/Cli -- --json                 # імпорт data/sample.csv, вивід одним JSON-рядком
dotnet run --project src/Cli -- --mixed                # data/mixed.csv: товари (P;) і клієнти (C;) одним switch (додаткове завдання 2)
dotnet run --project src/Cli -- --env                    # інформація про середовище (лаби 1-2)
dotnet run --project src/Cli -- --env --json              # те саме, у форматі JSON
```

Імпортер обирається автоматично за розширенням файлу (`.csv` → `ProductCsvImporter`,
`.json` → `ProductJsonImporter`) — обидва повертають однаковий `ImportResult<ProductDto>`.

Після кожного імпорту (звичайного або `--mixed`) виводиться статистика одним рядком:
`Усього: N, прийнято: X, пропущено: Y (Z% помилок)` (додаткове завдання 3).

## Публікація

```
dotnet publish src/Cli -c Release -r win-x64 --self-contained true  -o publish/win-x64-sc
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/win-x64-fd
```

| RID       | Режим               | Розмір publish | Потрібен встановлений runtime |
|-----------|---------------------|-----------------|--------------------------------|
| win-x64   | self-contained      | 76,86 МБ        | ні                              |
| win-x64   | framework-dependent | 0,19 МБ         | так (.NET 10)                   |

## Multi-targeting

`Core.csproj` і `Cli.csproj` таргетять `net8.0;net10.0`.

Заплановані підкаталоги в `Core` на наступні тижні:
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4)
- `Core/Storage/` — реалізації сховищ, DTO стануть форматом зберігання (тиждень 5)

## Середовище

.NET SDK 10.0, `Windows 10 Pro`
