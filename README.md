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
  src/
    Core/
      Core.csproj          # class library, multi-target: net8.0;net10.0
      EnvironmentInfo.cs    # EnvironmentReport + EnvironmentInfo.Collect()
    Cli/
      Cli.csproj            # ProjectReference -> Core
      Program.cs             # лише форматування виводу (таблиця / --json)
```

Залежність одностороння: `Cli → Core`. `Core` нічого не знає про `Cli`.

Заплановані підкаталоги в `Core` на наступні тижні (поки порожні, з'являться разом
із першим типом, що в них ляже):
- `Core/Dto/` — record-типи формату даних (тиждень 3): `ProductDto`, `OrderDto`
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4)
- `Core/Storage/` — реалізації сховищ (тиждень 5)

## Запуск

```
dotnet build
dotnet run --project src/Cli
dotnet run --project src/Cli -- --json
```

## Публікація

```
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
```

| RID       | Режим               | Розмір publish | Потрібен встановлений runtime |
|-----------|---------------------|-----------------|---------------------------------|
| win-x64   | self-contained      | 76,86 МБ        | ні                              |
| win-x64   | framework-dependent | 0,19 МБ         | так (.NET 10)                   |

Розмір каталогу (PowerShell):
```
(Get-ChildItem -Recurse <шлях до publish> | Measure-Object -Property Length -Sum).Sum / 1MB
```

## Multi-targeting

`Core.csproj` таргетить `net8.0;net10.0`. Якщо на машині немає SDK/targeting pack
для `net8.0`, залиште лише `<TargetFramework>net10.0</TargetFramework>` і зазначте
причину у звіті (`dotnet --list-sdks` покаже, які SDK встановлені).

## Середовище

.NET SDK 10.0, `<ваша ОС і архітектура>`
