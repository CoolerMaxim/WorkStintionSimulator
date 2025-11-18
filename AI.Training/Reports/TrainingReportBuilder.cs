using System.Text;
using AI.Training.Models;
using AI.Training.Evaluation;

namespace AI.Training.Reports;

public class TrainingReportBuilder
{
    public string BuildMarkdown(EvaluationReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# AI.Training Evaluation Report");
        sb.AppendLine();
        sb.AppendLine("## Quality Gates");
        sb.AppendLine($"- Macro F1: {report.ModelMetrics.MacroF1:F3}");
        sb.AppendLine($"- F1 (Critical): {report.ModelMetrics.CriticalF1:F3}");
        sb.AppendLine($"- Recall (Failed): {report.ModelMetrics.FailedRecall:F3}");
        sb.AppendLine();
        sb.AppendLine("## Baseline Gap");
        sb.AppendLine($"- Baseline Macro F1: {report.BaselineMetrics.MacroF1:F3}");
        sb.AppendLine($"- Improvement: {(report.ModelMetrics.MacroF1 - report.BaselineMetrics.MacroF1):F3}");
        sb.AppendLine();
        sb.AppendLine("## Class Distribution");
        foreach (var kvp in report.LabelDistribution.OrderBy(k => k.Key))
        {
            sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
        }

        return sb.ToString();
    }
}
