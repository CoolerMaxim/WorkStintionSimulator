# AI.Training Module Specification

## 1. Goal
Develop a training module that ingests validated telemetry datasets and trains a model to classify workstation HealthState. The module should deliver reproducible experiments, strong baselines, and room for future model upgrades.

## 2. Primary ML Task (v1)
* **Problem:** HealthState classification from a single telemetry row or a short window.
* **Classes:** `0=Normal`, `1=Degraded`, `2=Critical`, `3=Failed`.

## 3. Acceptance Criteria (Quality Gates)
A model release is valid only if it satisfies all of the following:
* **Macro F1 ≥ 0.85.**
* **F1(Critical) ≥ 0.90.**
* **Recall(Failed) ≥ 0.95** (favor false alarms over missed failures).
* **Baseline gap:** must exceed a rule-based baseline by **≥ +10% Macro F1**.

## 4. Temporal Split (mandatory)
* Sort by timestamp; use the first **70%** for training and the final **30%** for testing to avoid future leakage.

## 5. Reproducibility & Versioning
Persist `TrainingMetadata.json` capturing:
* `GeneratorVersion`, `DataQualityVersion`, `TrainingVersion`.
* Training timestamp and random seed.
* Model hyperparameters and feature list.
* Target (`HealthState` / `AnomalyType`).
* Train/test summary statistics.
* Quality metrics and full experiment logs.

## 6. Baseline Model (required)
Implement a rule-based baseline operating on the same features. Example rules:
* `BatteryVoltage < 22.5 → Critical`.
* `NetworkLatency > 2000 ms → Failed`.
* `AmplifierOutPower < expected × 0.5 → Degraded`.
* `InsideTemperature > 70 → Critical`.
Use this baseline for reporting and the +10% Macro F1 improvement check.

## 7. Module Breakdown
* **DatasetLoader:** load validated CSV, normalize/scale, apply temporal split.
* **FeatureExtractor:** compute base features, derived deltas/min/max, and short windows (6–12 steps).
* **LabelProcessor:** generate `HealthState`, handle class balancing.
* **ModelTrainer:** select optimal model (LightGBM/GBDT recommended), perform grid/random search and cross-validation.
* **ModelEvaluator:** compute metrics, confusion matrix, feature importance, and compare to baseline.
* **ModelExporter:** export ONNX, feature schema, and metadata.
* **TrainingReportBuilder:** produce Markdown/HTML and JSON reports.
* **TrainingPipeline:** orchestrate the above stages.

## 8. ML Quality Enhancements
* **Class separability analysis:** compare feature distributions; verify HealthState separability.
* **Data leakage checks:** ensure no feature directly encodes the label or anomaly type.
* **Pre-failure trend analysis:** track voltage decay, temperature spikes, network degradation, amplifier effectiveness.
* **Multi-factor analysis:** design cross-features for combined fault modes.
* **Scenario coverage:** ensure balanced representation of easy/normal/hard phases and pre/post-repair periods.

## 9. Model Choice (v1)
* Favor **LightGBM / gradient boosting on trees** for the initial release, with an upgrade path to more advanced models in v2+.
