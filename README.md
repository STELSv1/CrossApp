# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Замовлення**.
Сутності: `Customer` (клієнт), `Product` (товар), `Order` (замовлення), `OrderLine` (рядок замовлення).
Призначення: оформлення замовлень клієнтів і підрахунок сум.

## Запуск

```
dotnet build
dotnet run --project src/Cli
```

Вивід у вигляді таблиці (за замовчуванням) або одним JSON-рядком:

```
dotnet run --project src/Cli -- --json
```

## Середовище

.NET SDK 10.0, `<ваша ОС і архітектура, наприклад Windows 11 x64 / Ubuntu 24.04 x64>`

## Публікація self-contained (додаткове завдання)

```
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
```

Розмір каталогів publish:

| RID          | Розмір каталогу publish |
|--------------|--------------------------|
| win-x64      | 76,8 МБ                |
| linux-x64    | 78,8 МБ                |

Розмір каталогу можна отримати командами:
- Windows (PowerShell): `(Get-ChildItem -Recurse .\src\Cli\bin\Release\net10.0\win-x64\publish | Measure-Object -Property Length -Sum).Sum / 1MB`
- Linux/macOS: `du -sh src/Cli/bin/Release/net10.0/linux-x64/publish`
