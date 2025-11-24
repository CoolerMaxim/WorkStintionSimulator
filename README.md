# TelemetryGenerator

Набір проєктів для генерації синтетичної телеметрії, перевірки її якості та підготовки тренувальних датасетів.

## Структура репозиторію
- `TelemetryGenerator.Core` — бібліотека ядра для моделювання вузла, сценаріїв та аномалій.
- `TelemetryGenerator.Cli` — консольна утиліта, що використовує `TelemetryGenerator.Core` і експортує телеметрію у CSV.
- `TelemetryGenerator.DataQualityChecker` — бібліотека для перевірки структури та якості згенерованих CSV.
- `AI.Training` — допоміжні матеріали для побудови та оцінки ML-пайплайнів на телеметрії.
- `PipelineRunner` — оркестратор, який запускає побудову рішень, генерацію телеметрії, перевірку якості та тренування моделей.
- `docs/` — нотатки щодо AI/ML-підходів.

## Швидкий старт
1. **Збірка рішення**
   ```bash
   dotnet build TelemetryGenerator.sln
   ```
2. **Генерація телеметрії через CLI**
   ```bash
   dotnet run --project TelemetryGenerator.Cli/TelemetryGenerator.Cli.csproj -- \
     --scenario normal-day \
     --duration 24h \
     --step-minutes 5 \
     --workstation-id WS-001 \
     --output ./out/telemetry.csv
   ```
3. **Запуск пайплайну** (послідовно build → telemetry → check → train)
   ```bash
   dotnet run --project PipelineRunner/PipelineRunner.csproj -- --all
   ```
   Налаштування за замовчуванням можна змінити у `PipelineRunner/appsettings.json`.

## Додаткові ресурси
Більш детальні примітки щодо AI/ML підходів — у каталозі `docs/`.
