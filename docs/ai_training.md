# AI.Training Module Specification

## 1. Goal
Develop a training module that ingests validated telemetry datasets and trains a model to classify workstation HealthState. The module should deliver reproducible experiments, strong baselines, and room for future model upgrades. This document now adds implementation-oriented detail so the first training run can be built without guessing.

## 2. Primary ML Task (v1)
* **Problem:** HealthState classification from a single telemetry row or a short window.
* **Classes:** `0=Normal`, `1=Degraded`, `2=Critical`, `3=Failed`.
* **Input cadence:** assume 1–5s sampling; windowing should tolerate missing steps by forward-filling up to 2 intervals.
* **Latency budget:** offline training only; online inference target <50 ms on CPU for a single row.

## 3. Data Contract & Quality
* **Required columns (examples):** `timestamp`, `BatteryVoltage`, `NetworkLatencyMs`, `InsideTemperature`, `AmplifierOutPower`, `ExpectedAmplifierPower`, `CpuLoad`, `MemoryUsage`, `DiskHealthScore`, `FanRpm`, `SignalToNoise`, `SiteId`, `DeviceId`.
* **Expectations before training:**
  * Missingness <5% for core sensors (`BatteryVoltage`, `NetworkLatencyMs`, `InsideTemperature`).
  * Remove rows with non-monotonic timestamps per device; enforce UTC.
  * Cap extreme outliers at 1st/99th percentile per feature after per-site standardization.
  * Ensure `DeviceId` has ≥2 distinct time segments in both train and test to avoid single-device bias.

## 4. Acceptance Criteria (Quality Gates)
A model release is valid only if it satisfies all of the following:
* **Macro F1 ≥ 0.85.**
* **F1(Critical) ≥ 0.90.**
* **Recall(Failed) ≥ 0.95** (favor false alarms over missed failures).
* **Baseline gap:** must exceed a rule-based baseline by **≥ +10% Macro F1**.
* **Stability:** score variance across 3 random seeds ≤0.02 Macro F1 (use same temporal split but resample class weights / CV folds).

## 5. Temporal Split (mandatory)
* Sort by timestamp per device; use the first **70%** for training and the final **30%** for testing to avoid future leakage.
* Hold back the most recent **5%** as a shadow validation set to sanity-check drift between train/test (no tuning on it).

## 6. Reproducibility & Versioning
Persist `TrainingMetadata.json` capturing:
* `GeneratorVersion`, `DataQualityVersion`, `TrainingVersion`.
* Training timestamp and random seed.
* Model hyperparameters and feature list.
* Target (`HealthState` / `AnomalyType`).
* Train/test summary statistics.
* Quality metrics and full experiment logs.
* Artifact paths (model, ONNX, scaler, encoders) with checksums.
* CV split description (seed, fold count, temporal boundaries per fold).

## 7. Baseline Model (required)
Implement a rule-based baseline operating on the same features. Example rules:
* `BatteryVoltage < 22.5 → Critical`.
* `NetworkLatencyMs > 2000 → Failed`.
* `AmplifierOutPower < ExpectedAmplifierPower × 0.5 → Degraded`.
* `InsideTemperature > 70 → Critical`.
* `SignalToNoise < 5 → Degraded`; `SignalToNoise < 2 → Critical`.
* `CpuLoad > 95 and MemoryUsage > 90 → Critical`.
* Default to **Normal** if no rule fires.
* **Rule evaluation order:** highest severity first (Failed → Critical → Degraded → Normal) to keep behavior deterministic.
Use this baseline for reporting and the +10% Macro F1 improvement check.

## 8. Module Breakdown (with extra guidance)
* **DatasetLoader:** load validated CSV, normalize/scale, apply temporal split, and persist train/test/val statistics (mean/std, min/max per feature). Prefer `StandardScaler` for continuous signals and target encoding for categorical site/device IDs.
* **FeatureExtractor:** compute base features, deltas, rate-of-change, rolling min/max/mean (window 6–12 steps), last-observation carried forward for gaps, and optional z-score normalization per site. Explicitly log the feature schema and ordering.
* **LabelProcessor:** generate `HealthState`, handle class balancing (class weights and/or SMOTE for train only), and record label distribution before/after balancing.
* **ModelTrainer:** select optimal model (LightGBM/GBDT recommended), perform random/grid search over depth, learning rate, feature fraction, and class weights. Use **time-series cross-validation** (expanding window) with early stopping on shadow val.
* **ModelEvaluator:** compute metrics, confusion matrix, feature importance (gain- and permutation-based), calibration curves, and compare to baseline. Flag degradations vs previous best run.
* **ModelExporter:** export ONNX, feature schema, metadata, class mapping, and preprocessing steps (scalers/encoders).
* **TrainingReportBuilder:** produce Markdown/HTML and JSON reports containing metrics table, confusion matrix plot, feature importance chart, and drift checks between train/test/val.
* **TrainingPipeline:** orchestrate stages; fail fast if any quality gate or data contract check fails.

## 9. ML Quality Enhancements
* **Class separability analysis:** compare feature distributions; verify HealthState separability. Plot per-class KDEs for top features and ensure overlap is low for target-critical sensors.
* **Data leakage checks:** ensure no feature directly encodes the label or anomaly type. Run mutual information vs label and inspect unusually high signals tied to process artifacts.
* **Pre-failure trend analysis:** track voltage decay, temperature spikes, network degradation, amplifier effectiveness. Add slope-based features over the past 3–6 steps.
* **Multi-factor analysis:** design cross-features for combined fault modes (e.g., `HighTemp_AND_LowSNR`).
* **Scenario coverage:** ensure balanced representation of easy/normal/hard phases and pre/post-repair periods; stratify CV folds by site to avoid leakage.
* **Monitoring hooks:** log per-feature drift metrics (PSI or KS) between train and test, and warn if exceeding thresholds.

## 10. Model Choice (v1)
* Favor **LightGBM / gradient boosting on trees** for the initial release, with an upgrade path to more advanced models in v2+.
* Recommended starting hyperparameters: `num_leaves=63`, `max_depth=8`, `learning_rate=0.05`, `n_estimators=400`, `feature_fraction=0.8`, `bagging_fraction=0.8`, `class_weight=balanced`.
* Calibrate probabilities with Platt scaling or isotonic regression on the shadow validation set to stabilize alerting thresholds.