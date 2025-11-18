# WorkStintionJobSimulator
Симулятор роботи "коробки" (робочої станції), яка живе в умовах:

- повітряних тривог;
- відключень світла;
- можливих комбінованих сценаріїв (відключення світла + тривога).

Проєкт показує:

- як **генеруються події** з різною ймовірністю;
- як **відділити логіку сценаріїв** (події) від **фізики коробки** (живлення, тривога, батарея, тощо);
- як **зручно логувати** те, що відбувається.

---

### Для того щоб ознайомитись з проектом
https://docs.google.com/document/d/1RM_Mu7vpCMjSV0Mt0SGVZ-eLMSzuFFlH037e1fLFFHs/edit?usp=sharing

---

## Структура репозиторію
- `WorkstationJobSimulator` — консольний застосунок із симуляцією робочої станції та фізикою подій.
- `TelemetryGenerator.Core` — бібліотека для генерації телеметрії з моделями вузла, аномаліями та сценаріями.
- `TelemetryGenerator.Cli` — CLI-утиліта, що обгортає `TelemetryGenerator.Core` і формує CSV.
- `TelemetryGenerator.DataQualityChecker` — бібліотека для перевірки згенерованих CSV (структура, час, фізика, аномалії).
- `AI.Training` — допоміжні матеріали для ML/AI-тренувань на телеметрії.

## Налаштування за замовчуванням
### WorkstationJobSimulator
`Models/SimulationParameters.cs` задає стартові параметри симуляції:
- Назва станції: `"Робоча станція №1"`.
- Кількість ітерацій: `5`.
- Інтервал між подіями: від `20` до `40` секунд.
- Затримка перед стартом: `2` секунди для перегляду конфігурації перед запуском.

### TelemetryGenerator.Core
- Конфігурація вузла (`NodeConfig.CreateDefault`):
  - Ємність батареї `26 Ah`, cutoff `21.0 V`, повна `27.5 V`.
  - Струми: MCU `0.125 A`, мережевий `0.125 A`, динаміки `0.5 A`, зарядка від мережі `3.0 A`.
  - Версії: прошивка `FW-1.0.0`, ПЗ `APP-1.0.0`, ревізія заліза `HW-1`.
- Конфіг аномалій (`AnomalyConfigurationFactory.CreateDefault`):
  - Комбінації аномалій трапляються з імовірністю `0.35`.
  - Тривалості для кожного типу аномалій задаються діапазонами по складності (наприклад, `BatteryCutoff` 30–180 хв для `Normal`).
  - Ваги вибору аномалій: від `0.6` (`TempSensorFailure`) до `1.5` (`CoolingDegradation`).
  - Залежності, що тригерять вторинні аномалії (наприклад, `CoolingDegradation` → `TempSensorFailure`/`NetDegradation` із затримкою 20–120 хв).

### TelemetryGenerator.Cli
CLI підхоплює дефолтний словник аргументів, якщо не задати їх вручну:
- Сценарій: `normal-day`.
- Складність: `Normal`.
- Початок: поточний ISO-час (`DateTime.Now`).
- Тривалість моделювання: `24h`.
- Крок вибірки: `5` хвилин (`--step-minutes 5`).
- Ідентифікатор станції: `WS-001`.
- Кількість динаміків: `4`.
- Профіль вузла: `randomized` (конфігурація вузла береться з `NodeConfig.CreateDefault`).
- Шлях до вихідного CSV: `./telemetry.csv` у робочій директорії.

### TelemetryGenerator.DataQualityChecker
Бібліотека запускає набір аналізаторів із вбудованими порогами:
- `TimeGridAnalyzer`: допустиме відхилення кроку часу ±`2` хвилини; попереджає про дублікати таймстемпів.
- `PhysicsAnalyzer`: контроль діапазонів (наприклад, батарея 21–27.5 V, CPU 0–100 °C, температура в корпусі -10…80 °C, сигнал -110…-40 dBm, мережеві затримки 0–2000 мс або `9999` як тайм-аут).
- `AnomalyAnalyzer`: відстежує частку аномалій, пропущені типи та одинокі сплески без сусідів.
- `ClassBalanceAnalyzer`: мінімум `10` зразків на кожний `HealthState` та `AnomalyType` (крім `None`).
- `ScenarioAnalyzer`: усі `Difficulty` мають бути присутні; `ScenarioId`/`HealthState` не повинні бути порожніми.
- `MLFitnessAnalyzer`: агрегує аналіз балансів, трендів, мультифейлів тощо, щоб оцінити придатність до тренувань.

### AI.Training
- Сід генератора ML.NET: `7` (через `TrainingPipeline(seed)`), що гарантує повторюваність.
- Завантаження датасету: `TextLoader` читає CSV із заголовком та колонками, за замовчуванням відкидає записи без `HealthState`.
- Спліт даних: хронологічно `70%` train / `30%` test.
- Ознаки: напруга батареї, температури CPU/всередині, вихідна потужність підсилювача, мережеві затримки, кількість динаміків, аптайм/напрацювання та швидкість рестартів за 24 години.
- Бейзлайн: евристичні правила (наприклад, `NetworkLatencyMs > 2000` → `Failed`, батарея < 22.5 V або температура > 70 °C → `Critical`).
- Класифікатор: LightGBM Multiclass із 63 листками, `learningRate=0.05`, `iterations=400`, `minSamplesPerLeaf=10`, нормалізація середнього/дисперсії та кешування чекпойнту.
- Метадані експорту: версії артефактів/даних `v1`, список ознак та метрики (`MacroF1`, `CriticalF1`, `FailedRecall`) записуються в `TrainingReport.md` і JSON.

## Порядок запуску
1. **Підготовка**: потрібен .NET 8 SDK. Відновіть та зберіть всі проєкти з однієї точки:
   ```bash
   dotnet restore TelemetryGenerator.sln
   dotnet build TelemetryGenerator.sln
   ```
   Ця збірка охоплює `TelemetryGenerator.Core`, CLI, DataQualityChecker та `AI.Training` (перевірено, що вона проходить у Debug).

2. **PipelineRunner** (автоматизований сценарій): послідовно виконує `build → telemetry → check → train → simulate`.
   - За замовчуванням (або з прапорцем `--all`) запускає всі етапи вказаним порядком; окремі прапорці (`--build`, `--telemetry`, `--check`, `--train`, `--simulate`) дозволяють вибрати підмножину.
   - Базові аргументи (`--scenario`, `--duration`, `--step-minutes`, `--workstation-id`, `--output`) передаються у `TelemetryGenerator.Cli`, а шлях `--output` повторно використовується у `DataQualityChecker` та `AI.Training` — переконайтеся, що вказуєте його однаково, щоб усі етапи працювали з однією і тією ж CSV.
   - Приклади:
     ```bash
     # Повний прохід усіх кроків
     dotnet run --project PipelineRunner/PipelineRunner.csproj -- --all

     # Генерація + якість + тренування з власними параметрами
     dotnet run --project PipelineRunner/PipelineRunner.csproj -- \
       --telemetry --check --train \
       --scenario powerloss \
       --duration 12h \
       --step-minutes 10 \
       --workstation-id WS-002 \
       --output ./out/powerloss.csv \
       --training-output ./out/training

     # Тільки збірка та симуляція фізики
     dotnet run --project PipelineRunner/PipelineRunner.csproj -- --build --simulate
     ```

3. **Згенерувати телеметрію** через `TelemetryGenerator.Cli` (можна змінювати аргументи, інакше спрацюють значення за замовчуванням):
   ```bash
   dotnet run --project TelemetryGenerator.Cli/TelemetryGenerator.Cli.csproj -- \
     --scenario normal-day \
     --difficulty Normal \
     --start 2024-01-01T08:00:00 \
     --duration 24h \
     --step-minutes 5 \
     --workstation-id WS-001 \
     --speakers-configured 4 \
     --output ./out/telemetry.csv
   ```

4. **Перевірити CSV** за допомогою `TelemetryGenerator.DataQualityChecker`: після збірки DLL лежать у `TelemetryGenerator.DataQualityChecker/bin/Debug/net8.0/`.
   Швидкий варіант (за наявності `dotnet-script`):
   ```bash
   dotnet script - <<'CSHARP'
   #r "TelemetryGenerator.Core/bin/Debug/net8.0/TelemetryGenerator.Core.dll"
   #r "TelemetryGenerator.DataQualityChecker/bin/Debug/net8.0/TelemetryGenerator.DataQualityChecker.dll"
   using TelemetryGenerator.DataQualityChecker;

   var checker = new DataQualityChecker();
   var (md, json, report) = checker.Run("./out/telemetry.csv", "demo-dataset");
   Console.WriteLine(md);
   CSHARP
   ```
   Якщо `dotnet-script` недоступний, додайте посилання на проєкти в будь-якому власному консольному застосунку та викличте `DataQualityChecker.Run()` аналогічно.

5. **Навчити модель** з каталогу `AI.Training` (припускаючи наявність перевіреного CSV):
   ```bash
   dotnet script - <<'CSHARP'
   #r "TelemetryGenerator.Core/bin/Debug/net8.0/TelemetryGenerator.Core.dll"
   #r "AI.Training/bin/Debug/net8.0/AI.Training.dll"
   using AI.Training.Pipeline;

   var pipeline = new TrainingPipeline();
   var (report, modelPath, metadataPath) = pipeline.Run("./out/telemetry.csv", "./training-output");
   Console.WriteLine($"MacroF1={report.ModelMetrics.MacroF1:F3}, model={modelPath}");
   CSHARP
   ```
   Аналогічний код можна вставити у власний консольний застосунок; у каталозі `training-output` з'являться `model.zip`, `metadata.json` та `TrainingReport.md`.

6. **(Опційно) Переглянути фізичну симуляцію**: запустіть базовий `WorkstationJobSimulator` для демонстрації ітерацій подій.
   ```bash
   dotnet run --project WorkstationJobSimulator.csproj
   ```

## TelemetryGenerator.DataQualityChecker: налаштування і запуск
- Залежить від типів `TelemetryGenerator.Core`, тому використовуйте DLL після збірки рішення `TelemetryGenerator.sln`.
- Очікує CSV з заголовком, як генерує CLI (колонки `Timestamp`, `WorkStationId`, метрики стану, поля аномалій). Порожні `WorkStationId` або пропущені стовпці стають критичними помилками структури.
- Виклик: `var (markdown, json, report) = new DataQualityChecker().Run(csvPath, datasetName);`.
- Вихід: markdown-звіт (читабельний опис), JSON (машиночитний), модель `DataQualityReport` з метриками та списком `AnalyzerResult`.
- Пороги/діапазони за замовчуванням описані вище у блоці "TelemetryGenerator.DataQualityChecker"; їх можна розширити власними аналізаторами, додаючи нові сервіси на кшталт `AnalyzerResult`.

## AI.Training: налаштування і запуск
- Використовує той самий формат CSV, що й DataQualityChecker; записи без `HealthState` відкидаються завантажувачем.
- Основні точки налаштування: сід (`TrainingPipeline(seed)`), LightGBM-гіперпараметри в `ModelTrainer`, список фіч у `LabelProcessor`.
- Швидкий запуск: створіть `TrainingPipeline`, передайте шлях до CSV та вихідний каталог. Артефакти (`model.zip`, `metadata.json`, `TrainingReport.md`) зберігаються у вказаному каталозі.
- Метрики (MacroF1, CriticalF1, FailedRecall) порівнюються з бейзлайном правил, описаних у розділі про налаштування за замовчуванням.
- Для експорту в ONNX або інтеграції з іншими сервісами використовуйте `ModelExporter` з каталогу `Export` — він уже підключений у пайплайні.

## Типові конфігурації
- **Змінити тривалість або крок симуляції CLI:** передайте інші значення в `--duration` (наприклад, `12h` або `2d`) і `--step-minutes`.
- **Запустити інший сценарій:** параметр `--scenario` підтримує `normal-day`, `powerloss`, `speakers-degradation`, `net-failure`, `cooling-service` та їхні синоніми.
- **Фіксована конфігурація вузла:** використайте `--node-profile fixed`, щоб застосувати базову конфігурацію замість рандомізованої.
