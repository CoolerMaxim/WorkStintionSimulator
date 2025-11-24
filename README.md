# TelemetryGenerator

Набір взаємопов'язаних проєктів для моделювання поведінки виробничих робочих станцій, створення синтетичної телеметрії, перевірки її якості та підготовки тренувальних датасетів для ML-рішень. Телеметрія генерується за сценаріями, що імітують нормальну роботу та аномалії, а потім проходить автоматичну перевірку перед передачею в пайплайн побудови моделей.

## Структура репозиторію
- `TelemetryGenerator.Core` — ядро генератора. Описує сутності вузла, конфігурацію сценаріїв та моделі аномалій, надає API для побудови часових рядів.
- `TelemetryGenerator.Cli` — бібліотека сервісів запуску генерації, яку можна підключити через DI та викликати із застосунків без консольного парсера.
- `TelemetryGenerator.DataQualityChecker` — бібліотека, яка валідує згенеровані дані: структуру колонок, діапазони значень та наявність пропусків.
- `AI.Training` — приклади побудови й оцінки ML-пайплайнів на згенерованій телеметрії (Jupyter notebooks, конфігурації експериментів).
- `PipelineRunner` — оркестратор, який автоматизує повний цикл: збірку рішень, генерацію телеметрії, перевірку якості, підготовку датасетів і тренування моделей.
- `docs/` — нотатки щодо AI/ML-підходів і внутрішніх досліджень.

## Швидкий старт
1. **Збірка рішення**
   ```bash
   dotnet build TelemetryGenerator.sln
   ```
2. **Генерація телеметрії у власному сервісі**
   Додайте `TelemetryGenerationRunner` у DI та викличте його у будь-якому застосунку (web API, бекграунд-воркер тощо). Приклад мінімального API:

   ```csharp
   using Microsoft.AspNetCore.Builder;
   using Microsoft.Extensions.DependencyInjection;
   using Microsoft.Extensions.Hosting;
   using TelemetryGenerator.Cli;

   var builder = WebApplication.CreateBuilder(args);
   builder.Services.AddTelemetryGenerationRunner();

   var app = builder.Build();

   app.MapPost("/generate", async (TelemetryGenerationRequest request, TelemetryGenerationRunner runner, CancellationToken ct) =>
   {
       await runner.RunAsync(request, ct);
       return Results.Accepted();
   });

   await app.RunAsync();
   ```
   Запит `POST /generate` приймає JSON з полями `TelemetryGenerationRequest`, виконує `RunAsync` та коректно обробляє `CancellationToken` без консольного парсера.
3. **Запуск пайплайну** (послідовно build → telemetry → check → train)
   ```bash
   # явний вибір кроків через CLI (RunAll вимкнено за замовчуванням)
   dotnet run --project PipelineRunner/PipelineRunner.csproj -- --steps:0 build --steps:1 telemetry --steps:2 check --steps:3 train

   # або аналогічно, але одним прапорцем
   dotnet run --project PipelineRunner/PipelineRunner.csproj -- --runAll true
   ```
   Налаштування за замовчуванням можна змінити у `PipelineRunner/appsettings.json`.

## Основні параметри генерації
**TelemetryGenerationRequest** описує параметри генерації, які можна передати у веб‑ендпоінт чи сервіс:

- `Scenario` — назва сценарію (наприклад, `normal-day`, `overheat`, `power-surge`). Визначає набір аномалій та частоту їх появи.
- `Duration` — загальний час симуляції у форматі `1h`, `24h`, `3d` тощо. Визначає довжину часових рядів.
- `StepMinutes` — інтервал між точками вимірювань у хвилинах. Менші значення дають більше точок та більший обсяг файлу.
- `WorkstationId` — логічне ім'я/ID вузла, яке буде проставлене у кожному рядку CSV для простішого фільтрування.
- `OutputPath` — шлях до цільового CSV-файлу. Директорії створюються автоматично, якщо їх немає.
- `Seed` — фіксує генератор випадкових чисел. З однаковим seed та параметрами дані будуть тотожними.
- `Difficulty`, `NodeProfile`, `SpeakersConfigured` — додаткові параметри для налаштування характеру аномалій та профілю вузла.

## Робота з пайплайном
**PipelineRunner** автоматизує типовий сценарій використання:

1. **Збірка** — відновлює залежності та компілює рішення.
2. **Генерація** — використовує `TelemetryGenerationRunner` із параметрами з `appsettings.json` (можна змінювати тривалість, сценарії та вихідні шляхи).
3. **Перевірка якості** — запускає `TelemetryGenerator.DataQualityChecker`, який аналізує створений файл на наявність пропусків, некоректних типів та викидів за межі допустимих діапазонів.
4. **Тренування** — передає очищені дані в `AI.Training` для побудови моделей і збереження метрик.

Ключові параметри `PipelineRunner` задаються в `PipelineRunner/appsettings.json`. Типові поля:
- `Telemetry:Scenario` — сценарій для генерації;
- `Telemetry:Duration` і `Telemetry:StepMinutes` — аналогічно полям `TelemetryGenerationRequest`;
- `Telemetry:OutputPath` — шлях до тимчасових або постійних датасетів;
- `Training:Enabled` — дозволяє пропустити або виконати етап ML;
- `Training:RunName` — підпис експерименту для подальшої ідентифікації.

Параметри керування кроками пайплайну задаються явними прапорцями або аргументами командного рядка:

- `RunAll` (`--runAll` або скорочено `--all`) — запускає всі кроки; за замовчуванням вимкнено.
- `Build` / `Telemetry` / `Check` / `Train` — окремі прапорці для виконання відповідних кроків.
- `Steps` — список кроків (наприклад, `["build", "telemetry"]` в `appsettings.json` або `--steps:0 build --steps:1 telemetry` у CLI). Значення нечутливі до регістру, допускаються коми/крапки з комою для короткого запису в одному аргументі.

Пайплайн виконує крок, якщо ввімкнено `RunAll`, установлено відповідний прапорець або крок явно присутній у `Steps`. Це дозволяє точно визначити потрібні етапи через конфіг або командний рядок і уникнути неочікуваного запуску за замовчуванням.

## Додаткові ресурси
Більш детальні примітки щодо AI/ML-підходів — у каталозі `docs/`. За потреби можна комбінувати CLI та PipelineRunner, щоб швидко підготувати контрольні датасети з різними сценаріями і повторно використати їх у навчанні.
