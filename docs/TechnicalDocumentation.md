# TelemetryGenerator — технічна документація

## 1. Огляд проєкту
- **Рішення**: `TelemetryGenerator.sln` із пов’язаними допоміжними рішеннями (`TelemetryGenerator.Build.slnf`).
- **Призначення**: моделювання роботи виробничих вузлів, генерація синтетичної телеметрії, перевірка її якості та підготовка датасетів для подальшого ML‑тренування.
- **Основні завдання**: 
  - Формування часових рядів телеметрії за сценаріями з нормальними та аномальними режимами.
  - Автоматична валідація згенерованих CSV‑файлів (структура, часові сітки, фізичні обмеження, узгодженість сценаріїв, придатність для ML).
  - Оркестрація повного циклу (build → telemetry → quality check → training) через CLI.

## 2. Структура рішення
### Проєкти
- **TelemetryGenerator.Core** — ядро симулятора: моделі стану, аномалії, сценарії, модулі обладнання, генератор часових рядів.
- **TelemetryGenerator.Generation** — сервісна бібліотека, що збирає DI‑компоненти, реєструє сценарії та записує CSV.
- **TelemetryGenerator.Cli** — оболонка для запуску генерації з DI або веб/CLI: перетворення запиту на канонічні `GenerationConfig`/`SimulationConfig`, валідація вводу, конфіг з `SimulationSettings`.
- **TelemetryGenerator.DataQualityChecker** — набір аналізаторів якості даних та конструктор звітів у Markdown/JSON.
- **PipelineRunner** — консольний оркестратор кроків build/telemetry/check/train з налаштуванням через `appsettings.json` та аргументи CLI.
- **AI.Training** — допоміжні конфігурації та ноутбуки для експериментів з ML (не містить виконуваного коду в цій репрезентації).

### Основні простори назв / папки
- `TelemetryGenerator.Core.Configuration` — конфігурація вузла, параметри сценаріїв, дефолти.
- `TelemetryGenerator.Core.Modules` — модулі вузла (оточення, живлення, мережа, підсилювач тощо) з інтерфейсом `IModule`.
- `TelemetryGenerator.Core.Services` — фабрики моделей, інжектор аномалій, планувальник обслуговування, інтерфейс генерації телеметрії.
- `TelemetryGenerator.Core.Scenarios` — типові сценарії та фази.
- `TelemetryGenerator.Generation` — DI‑реєстрації, `TelemetryGenerationService`, провайдер модулів, фабрики аномалій/температур/акумулятора.
- `TelemetryGenerator.Cli` — `TelemetryGenerationRunner`, DTO запиту, розширення DI.
- `TelemetryGenerator.DataQualityChecker` — аналізатори, моделі результатів, завантажувач CSV, збирач звітів.
- `PipelineRunner.Options/Steps/Services` — налаштування пайплайну, валідатори, кроки виконання та джоби.

### Ключові класи
- `TelemetryGenerator.Core.TelemetryGenerator` — головний цикл симуляції: оновлює модулі, інжектує аномалії, планує сервіс, формує `TelemetrySample`.
- `SimulationSettings`, `TelemetryGenerationRequest` — адаптери вводу (рядкові/CLI) для канонічних `SimulationConfig` і `GenerationConfig`.
- `Scenario`, `ScenarioPhase`, `ScenarioFactory`, `ScenarioRegistry` — декларація сценаріїв і зв’язування назв з фабриками.
- `AnomalyConfiguration` та пов’язані профілі — параметризація тривалості/ваг аномалій і залежностей.
- `TelemetryGenerationService` — реалізація `ITelemetryGenerationService`: збирає конфіг, створює моделі, запускає генератор і пише CSV.
- `TelemetryGenerationRunner` — приймає запит, з’єднує його з `SimulationSettings`, перевіряє коректність і викликає сервіс.
- `DataQualityChecker` — керує набором аналізаторів і збирає звіт.
- `PipelineRunner` (`Program`, `PipelineOptions`, кроки Build/Telemetry/QualityCheck/Train) — визначає послідовність виконання й підхоплення налаштувань.

## 3. Основні сценарії роботи системи
### Генерація телеметрії
1. **Вхід**: `TelemetryGenerationRequest` (CLI/web) → `GenerationConfig` (канонічний контракт сервісу).
2. **Runner**: `GenerationConfigFactory.Create()` зливає запит із `SimulationSettings`, парсить тривалість/час старту, нормалізує шляхи, перевіряє валідність (порожні поля, крок > тривалості тощо) і логує параметри.
3. **Сервіс**: `TelemetryGenerationService.GenerateAsync()` створює `Random`, будує `NodeConfig` за профілем (`fixed`/`randomized`), фабрикує `AnomalyInjector`, `MaintenanceScheduler`, `BatteryModel`, `TemperatureModel`, модулі вузла й обирає сценарій через `ScenarioRegistry`.
4. **Цикл**: `TelemetryGenerator.Run()` проходить фази сценарію, на кожному кроці (TimeSpan `Step`) виконує: оновлення доступності живлення, `IModule.Update` для всіх модулів, оновлення батареї, інжекцію аномалій, планування сервісу, обчислення супервізорних сигналів (здоров’я ПЗ/вузла) і проєкцію в `TelemetrySample`.
5. **Вихід**: `TelemetryGenerationService` перетворює вибірку на CSV (рядок заголовка + значення з форматуванням) і зберігає у `OutputPath`.

### Перевірка якості даних
1. **Ініціатор**: `QualityCheckStep` у PipelineRunner або прямий виклик `DataQualityChecker`.
2. **Джоба**: `DataQualityJob.ExecuteAsync()` читає `DatasetSettings` (шляхи вхід/звіт), логувує конфіг `ValidationSettings`, створює директорію для звітів.
3. **Checker**: `DataQualityChecker.Run()` завантажує CSV через `CsvLoader`, проганяє активовані аналізатори (структура, часові сітки, фізика, аномалії, сценарії, ML‑фітнес). Результати консолідуються в `DataQualityReport` і класифікуються як `Pass/PassWithWarnings/Fail` за наявністю Critical/Warning.
4. **Вихід**: Markdown/JSON звіти на шляхи `QualityMarkdownPath` та `QualityJsonPath`; код виходу 2 при Fail, інакше 0.

### Пайплайн build → telemetry → check → train
1. **Старт**: `PipelineRunner` читає `PipelineOptions` з `appsettings.json` + аргументів CLI (`--all`, `--build`, `--steps:0 telemetry`, тощо) і валідує опції.
2. **Кроки**: послідовність `(build, telemetry, check, train)` створюється у `Program`. Кожен крок виконується лише якщо ввімкнений `RunAll`, відповідний прапорець або явна наявність у `Steps`.
3. **Build**: (реалізація не показана) має будувати solution filter `TelemetryGenerator.Build.slnf`.
4. **Telemetry**: `TelemetryStep` формує `TelemetryGenerationRequest` з `PipelineOptions` і викликає Runner.
5. **Check**: `QualityCheckStep` запускає `DataQualityJob`.
6. **Train**: `TrainStep` (через `TrainingJob`) готує датасет і запускає ML‑конвеєр (конфіг у `TrainingSettings`).
7. **Вихід**: логування кожного кроку; перший ненульовий код завершує виконання.

## 4. Взаємодія модулів
- **Core** містить бізнес‑логіку симуляції (стан вузла, модулі обладнання, аномалії, сценарії).
- **Generation** інкапсулює побудову об’єктів Core та доступ до них через DI.
- **Cli** приймає зовнішні запити, агрегує конфіг і викликає Generation.
- **DataQualityChecker** працює з результатним CSV, без втручання у генерацію.
- **PipelineRunner** координує запуск усіх попередніх бібліотек.

```
[PipelineRunner] --calls--> [TelemetryGenerationRunner] --uses--> [TelemetryGenerationService]
        |                                           |
        |                               [ScenarioRegistry + Factories] --build--> [TelemetryGenerator.Core]
        |                                           |
        +---> [DataQualityJob] --uses--> [DataQualityChecker]
        +---> [TrainingJob] (ML) uses generated/validated datasets
```

Бізнес‑логіка (динаміка стану, правила аномалій, сценарії) живе в Core; взаємодія з файловою системою (CSV/звіти) — у Generation, DataQualityChecker, PipelineRunner.

## 5. Що налаштувати перед першим запуском
### `TelemetryGenerator.Cli/appsettings.json` → секція `Simulation`
| Параметр | Тип/дефолт | Призначення | Наслідок неправильного значення |
| --- | --- | --- | --- |
| `scenario` | `string`, `"normal-day"` | Ідентифікатор сценарію (`ScenarioRegistry`). | Порожнє значення викличе `InvalidOperationException` при валідації Runner. |
| `difficulty` | `string`, `"Normal"` | Рівень складності (`Difficulty`). | Непарсабельне значення → дефолт `Normal`. |
| `start` | `string`, пустий | Час початку (ISO/будь-який `DateTime`), пустий → `DateTime.Now`. | Некоректний формат → поточний час. |
| `duration` | `string`, `"24h"` | Тривалість симуляції (`DurationParser`). | Непарсабельне → 24h, нуль/від’ємне відкидається валідатором. |
| `stepMinutes` | `int`, `5` | Інтервал між точками. | ≤0 → помилка валідації; > тривалості → помилка. |
| `workstationId` | `string`, `"WS-001"` | Логічний ID вузла. | Порожнє → помилка. |
| `speakersConfigured` | `int`, `4` | К-сть динаміків у конфіг. | ≤0 → автоматично приводиться до ≥1 у Runner. |
| `nodeProfile` | `string`, `"randomized"` | Профіль конфігурації вузла (`fixed`/`randomized`). | Невідомий → використовується `randomized`. |
| `outputPath` | `string`, `"./telemetry.csv"` | Шлях до CSV; директорії створюються. | Порожнє → помилка. |
| `seed` | `int?`, `null` | Фіксація RNG. | <0 → помилка валідації опцій сервісу. |

### `PipelineRunner/appsettings.json`
Секції наслідують попередні поля `Simulation`, плюс:
- **Dataset**: `workingDirectory`, `outputPath`, `datasetName`, `qualityMarkdownPath`, `qualityJsonPath` — визначають, де лежить CSV та куди писати звіти; порожні шляхи призведуть до запису відносно робочої директорії.
- **ValidationSettings**: `enable*Analyzer` прапорці для кожного аналізатора DataQualityChecker.
- **Training**: `workingDirectory`, `outputDirectory`, `trainFraction`, `evaluationFraction`, `modelType`, `seed` — параметри ML‑пайплайну; валідація гарантує частки в (0,1].
- Глобальні прапорці: `solutionPath`, `telemetryProjectPath`, `runAll`, `build`, `telemetry`, `check`, `train`, `steps` — визначають шлях до sln/csproj і набір активних кроків.

### `TelemetryGenerator.DataQualityChecker/dataquality.appsettings.json`
- `dataQualityChecker.enable*Analyzer` — окреме конфіг-джерело для сервісу перевірки якості без PipelineRunner.

### ENV-змінні
- Явні ENV у коді відсутні; конфігурація надходить із `appsettings.json` та аргументів CLI.

### Критичні константи в коді
- `TelemetryDefaults` — дефолтні значення сценарію, тривалості, кроку, імені файлу та складності.
- `AnomalyConfigurationFactory` — тривалості, ваги та залежності аномалій (визначають частоту/довжину аномалій).
- Ймовірності відкриття/закриття дверей в `EnvironmentModule` (`0.02` та `0.3` на годину) впливають на термодинаміку та подальші метрики.

## 6. Як запускати проєкт
### Із IDE (VS/Rider/VS Code)
- Стартовий проєкт: `PipelineRunner` для повного циклу або `TelemetryGenerator.Cli` для окремої генерації.
- Профілі запускаються через стандартний `dotnet run`; аргументи CLI можна прописати в launchSettings або передати вручну (`--runAll true`, `--steps:0 telemetry`).

### З командного рядка
- **Побудова рішення**: `dotnet build TelemetryGenerator.sln`
- **Генерація телеметрії через PipelineRunner**:
  ```bash
  dotnet run --project PipelineRunner/PipelineRunner.csproj -- --steps:0 telemetry --steps:1 check
  ```
- **Повний пайплайн**:
  ```bash
  dotnet run --project PipelineRunner/PipelineRunner.csproj -- --runAll true
  ```
- **Використання Runner без консольного парсера**: додайте `TelemetryGenerationRunner` у DI веб‑API і викликайте `RunAsync(request)` у власному контролері/ендпоінті.

### Режими
- Окремі прапорці `--build`, `--telemetry`, `--check`, `--train` або масив `--steps` дозволяють вибирати будь-яку підмножину етапів; без `RunAll` за замовчуванням усе вимкнено.

## 7. Конфігуровані класи та властивості
- **`SimulationSettings`**: `Scenario`, `Difficulty`, `Start`, `Duration`, `StepMinutes`, `WorkstationId`, `SpeakersConfigured`, `NodeProfile`, `OutputPath`, `Seed` (дефолти: normal-day/Normal/24h/5 хв/WS-001/4/randomized/./telemetry.csv/null). Обов’язкові: Scenario, WorkstationId, Duration>0, StepMinutes>0.
- **`SimulationConfig`**: типізований контракт симуляції (без рядкових полів) із застосованими дефолтами/нормалізацією.
- **`GenerationConfig`**: `SimulationConfig` + `OutputPath`, `Seed` — канонічний контракт сервісу генерації.
- **`TelemetryGenerationRequest`**: DTO з сирими рядковими значеннями для CLI/web, мапиться у `GenerationConfig` через `GenerationConfigFactory`.
- **`PipelineOptions`**: дублює параметри Simulation + `SolutionPath`, `TelemetryProjectPath`, `WorkingDirectory`, прапорці `RunAll/Build/Telemetry/Check/Train`, список `Steps`.
- **`DatasetSettings`**: шляхи до вхідного CSV та звітів; похідні властивості `Resolved*` нормалізують шляхи.
- **`ValidationSettings` / `DataQualityCheckerOptions`**: прапорці `Enable*Analyzer` для підключення/вимкнення окремих перевірок.
- **`TrainingSettings`**: `WorkingDirectory`, `OutputDirectory`, `TrainFraction`, `EvaluationFraction`, `ModelType`, `Seed`; метод `ToPipelineOptions()` готує опції для ML‑пайплайну.
- **`DatasetConfig` / `TrainingConfig` / `PipelineConfig`**: канонічні типізовані агрегати для пайплайну (телеметрія + датасет + ML), будуються через `PipelineConfigFactory` з відповідних секцій `appsettings.json`.

## 8. Зовнішні залежності
- **NuGet**: `Microsoft.Extensions.DependencyInjection`/`Options`/`Logging` (через SDK) для DI, конфігурацій і логування; інших сторонніх ML/БД бібліотек у кодовій базі немає.
- **Зовнішні сервіси**: не використовуються; вся робота — з локальними файлами CSV і звітами.

## 9. Розширення та кастомізація
- **Новий тип вузла/профіль**: реалізувати генерацію конфігурації в `NodeConfigurationFactory` або додати нову гілку в `CreateNodeConfig` (`TelemetryGenerationService`).
- **Новий сценарій аномалій**: додати фабрику в `ScenarioFactory` і зареєструвати назву в `ScenarioRegistry.CreateScenarioRegistry()`.
- **Новий модуль обладнання**: реалізувати `IModule.Update`, додати його в `DefaultModuleProvider.CreateModules()` і передати потрібні моделі/параметри.
- **Нове правило аномалій**: розширити `AnomalyInjector`/`AnomalyConfigurationFactory` (тривалості, ваги, залежності) або додати новий тип у `AnomalyType`.
- **Нові метрики/звіти якості**: створити аналізатор, що повертає `AnalyzerResult`, і підключити його через DI та прапорці `Enable*Analyzer`.
- **Інший ML‑алгоритм**: змінити/розширити опції у `AI.Training` і логіку `TrainingJob`/`TrainStep` (при наявності коду тренування).

## 10. Короткий гайд для нового розробника
1. Клонувати репозиторій і виконати `dotnet restore`/`dotnet build TelemetryGenerator.sln`.
2. Ознайомитися з `docs/TechnicalDocumentation.md` та `README.md`.
3. Налаштувати `PipelineRunner/appsettings.json` під потрібний сценарій (шлях до CSV, увімкнені кроки, прапорці аналізаторів).
4. Запустити `dotnet run --project PipelineRunner/PipelineRunner.csproj -- --runAll true` для швидкої перевірки повного циклу.
5. Перевірити результати: CSV у `Dataset.OutputPath`, звіти якості у `Dataset.QualityMarkdownPath/QualityJsonPath`, логування в консолі.
6. Для інтеграції в інші сервіси — додати `TelemetryGenerationRunner` у DI та викликати `RunAsync` із власним `TelemetryGenerationRequest`.
7. Для ML‑експериментів — переглянути `AI.Training` і налаштувати `TrainingSettings`.
8. Для дебагу симуляції — звернути увагу на `TelemetryGenerator.Core.TelemetryGenerator` та модулі в `TelemetryGenerator.Core.Modules`.
9. Для зміни сценаріїв — редагувати `ScenarioFactory`/`ScenarioRegistry` і параметри аномалій у `AnomalyConfigurationFactory`.
10. Додаткові параметри дефолтів — у `TelemetryGenerator.Core.Configuration` (`TelemetryDefaults`, `SimulationSettings`).

**Глосарій**
- **WorkStation** — логічний вузол/станція, що генерує телеметрію.
- **Scenario / ScenarioPhase** — набір фаз поведінки вузла з тривалістю та діями (анімує норму/аварії/ремонт).
- **AnomalyInjector** — компонент, що вмикає/вимикає аномалії згідно з конфігурацією та складністю.
- **TelemetrySample** — запис телеметрії (часова мітка, стани обладнання, аномалії, здоров’я ПЗ та ін.).
- **DataQualityChecker** — сервіс, що перевіряє готовність датасету до ML.
- **PipelineRunner** — консольний оркестратор, який виконує кроки build/telemetry/check/train.
