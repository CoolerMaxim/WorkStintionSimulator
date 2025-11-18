using System.Globalization;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Services;

public sealed class CsvLoader
{
    private static readonly string[] RequiredColumns = new[]
    {
        "Timestamp",
        "WorkStationId",
        "PowerStatus",
        "BatteryStatus",
        "BatteryVoltage",
        "CpuTemperature",
        "InsideTemperature",
        "DiskSpaceUse",
        "DoorOpenStatus",
        "AmplifierStatus",
        "AmplifierOutPower",
        "SoundStatus",
        "SignalStrength",
        "NetworkLatency",
        "SpeakersConfigured",
        "IsAnomaly",
        "AnomalyType",
        "HealthState"
    };

    public CsvLoadResult Load(string path)
    {
        var issues = new List<AnalysisIssue>();
        var missingColumns = new List<string>();

        if (!File.Exists(path))
        {
            issues.Add(new AnalysisIssue(IssueSeverity.Critical, $"File not found: {path}"));
            return new CsvLoadResult(Array.Empty<TelemetryRecord>(), issues, Array.Empty<string>());
        }

        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            issues.Add(new AnalysisIssue(IssueSeverity.Critical, "CSV header is missing"));
            return new CsvLoadResult(Array.Empty<TelemetryRecord>(), issues, RequiredColumns);
        }

        var headers = headerLine.Split(',');
        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            headerLookup[headers[i].Trim()] = i;
        }

        foreach (var required in RequiredColumns)
        {
            if (!headerLookup.ContainsKey(required))
            {
                missingColumns.Add(required);
                issues.Add(new AnalysisIssue(IssueSeverity.Critical, $"Required column '{required}' is missing"));
            }
        }

        var records = new List<TelemetryRecord>();
        string? line;
        var lineNumber = 1; // already consumed header
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = line.Split(',');
            var record = ParseRecord(values, headerLookup, issues, lineNumber);
            if (record != null)
            {
                records.Add(record);
            }
        }

        return new CsvLoadResult(records, issues, missingColumns);
    }

    private static TelemetryRecord? ParseRecord(IReadOnlyList<string> values, IDictionary<string, int> lookup, ICollection<AnalysisIssue> issues, int lineNumber)
    {
        bool TryBool(string key, bool defaultValue = false)
        {
            return lookup.TryGetValue(key, out var idx) && idx < values.Count && bool.TryParse(values[idx], out var result)
                ? result
                : defaultValue;
        }

        double TryDouble(string key, double defaultValue = double.NaN)
        {
            if (lookup.TryGetValue(key, out var idx) && idx < values.Count)
            {
                if (double.TryParse(values[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                issues.Add(new AnalysisIssue(IssueSeverity.Warning, $"Invalid double value in column '{key}'", $"line {lineNumber}"));
            }
            return defaultValue;
        }

        int TryInt(string key, int defaultValue = 0)
        {
            if (lookup.TryGetValue(key, out var idx) && idx < values.Count)
            {
                if (int.TryParse(values[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                issues.Add(new AnalysisIssue(IssueSeverity.Warning, $"Invalid integer value in column '{key}'", $"line {lineNumber}"));
            }
            return defaultValue;
        }

        string TryString(string key)
        {
            return lookup.TryGetValue(key, out var idx) && idx < values.Count ? values[idx] : string.Empty;
        }

        if (!lookup.TryGetValue("Timestamp", out var tsIndex) || tsIndex >= values.Count || !DateTime.TryParse(values[tsIndex], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var timestamp))
        {
            issues.Add(new AnalysisIssue(IssueSeverity.Warning, "Invalid Timestamp value", $"line {lineNumber}"));
            return null;
        }

        var anomalyType = ParseEnum(values, lookup, "AnomalyType", AnomalyType.None, issues, lineNumber);
        var healthState = ParseEnum(values, lookup, "HealthState", HealthState.Nominal, issues, lineNumber);

        return new TelemetryRecord
        {
            Timestamp = timestamp,
            WorkStationId = TryString("WorkStationId"),
            PowerStatus = TryBool("PowerStatus"),
            BatteryStatus = TryBool("BatteryStatus"),
            BatteryVoltage = TryDouble("BatteryVoltage"),
            CpuTemperature = TryDouble("CpuTemperature"),
            InsideTemperature = TryDouble("InsideTemperature"),
            DiskSpaceUse = TryInt("DiskSpaceUse"),
            DoorOpenStatus = TryBool("DoorOpenStatus"),
            AmplifierStatus = TryBool("AmplifierStatus"),
            AmplifierOutPower = TryDouble("AmplifierOutPower"),
            SoundStatus = TryBool("SoundStatus"),
            SignalStrength = TryDouble("SignalStrength"),
            NetworkLatency = TryInt("NetworkLatency"),
            SpeakersConfigured = TryInt("SpeakersConfigured"),
            SpeakersEffective = lookup.TryGetValue("SpeakersEffective", out var effectiveIdx) && effectiveIdx < values.Count && int.TryParse(values[effectiveIdx], out var effective)
                ? effective
                : null,
            IsAnomaly = TryBool("IsAnomaly"),
            AnomalyType = anomalyType,
            HealthState = healthState,
            ScenarioId = TryString("ScenarioId"),
            Difficulty = TryString("Difficulty"),
            RestartCount = TryInt("RestartCount")
        };
    }

    private static TEnum ParseEnum<TEnum>(IReadOnlyList<string> values, IDictionary<string, int> lookup, string column, TEnum defaultValue, ICollection<AnalysisIssue> issues, int lineNumber)
        where TEnum : struct
    {
        if (lookup.TryGetValue(column, out var idx) && idx < values.Count)
        {
            if (Enum.TryParse(values[idx], true, out TEnum parsed))
            {
                return parsed;
            }

            issues.Add(new AnalysisIssue(IssueSeverity.Warning, $"Invalid enum value in column '{column}'", $"line {lineNumber}"));
        }
        return defaultValue;
    }
}
