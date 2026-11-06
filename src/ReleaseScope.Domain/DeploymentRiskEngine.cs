namespace ReleaseScope.Domain;

public sealed class DeploymentRiskEngine
{
    public RiskAssessment Assess(DeploymentSignals signals)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(signals.ChangedFiles);
        ArgumentOutOfRangeException.ThrowIfNegative(signals.LinesChanged);
        ArgumentOutOfRangeException.ThrowIfNegative(signals.FailedTests);
        ArgumentOutOfRangeException.ThrowIfNegative(signals.CriticalComponents);
        ArgumentOutOfRangeException.ThrowIfNegative(signals.SecurityFindings);

        var factors = new List<RiskFactor>();

        AddPositiveFactor(
            factors,
            "change-size",
            "Change size",
            Math.Min(20m, signals.LinesChanged / 100m + signals.ChangedFiles / 5m),
            $"{signals.ChangedFiles} files and {signals.LinesChanged} changed lines increase the review surface.");

        AddPositiveFactor(
            factors,
            "failed-tests",
            "Failed tests",
            signals.FailedTests == 0 ? 0 : Math.Min(30m, 18m + signals.FailedTests * 3m),
            signals.FailedTests == 0
                ? "The test suite is green."
                : $"{signals.FailedTests} automated tests are failing.");

        AddPositiveFactor(
            factors,
            "incident-history",
            "Recent incident rate",
            Math.Min(20m, Clamp(signals.RecentIncidentRate, 0m, 1m) * 20m),
            $"The affected area has a {signals.RecentIncidentRate:P0} recent incident rate.");

        AddPositiveFactor(
            factors,
            "critical-components",
            "Critical components",
            Math.Min(16m, signals.CriticalComponents * 6m),
            $"The deployment touches {signals.CriticalComponents} business-critical component(s).");

        AddPositiveFactor(
            factors,
            "performance-regression",
            "Performance regression",
            Math.Min(18m, Math.Max(0m, signals.PerformanceRegressionPercent) * 0.6m),
            $"Pre-production checks detected a {signals.PerformanceRegressionPercent:0.#}% latency regression.");

        AddPositiveFactor(
            factors,
            "security-findings",
            "Security findings",
            Math.Min(24m, signals.SecurityFindings * 8m),
            signals.SecurityFindings == 0
                ? "No unresolved security finding was reported."
                : $"{signals.SecurityFindings} unresolved security finding(s) require attention.");

        var coverageProtection = signals.TestCoveragePercent switch
        {
            >= 90m => -14m,
            >= 80m => -10m,
            >= 70m => -6m,
            _ => 0m
        };

        if (coverageProtection < 0)
        {
            factors.Add(new RiskFactor(
                "test-coverage",
                "Test coverage",
                coverageProtection,
                $"{signals.TestCoveragePercent:0.#}% coverage reduces uncertainty."));
        }

        if (signals.HasRollbackPlan)
        {
            factors.Add(new RiskFactor(
                "rollback-plan",
                "Verified rollback plan",
                -10m,
                "A tested rollback path limits the blast radius."));
        }

        var score = Math.Round(Clamp(factors.Sum(factor => factor.Impact), 0m, 100m), 1);
        var level = score switch
        {
            < 25m => RiskLevel.Low,
            < 50m => RiskLevel.Moderate,
            < 75m => RiskLevel.High,
            _ => RiskLevel.Critical
        };

        var decision = level switch
        {
            RiskLevel.Low => DeploymentDecision.Allow,
            RiskLevel.Moderate or RiskLevel.High => DeploymentDecision.ReviewRequired,
            _ => DeploymentDecision.Block
        };

        return new RiskAssessment(
            score,
            level,
            decision,
            factors.OrderByDescending(factor => factor.Impact).ToArray());
    }

    private static void AddPositiveFactor(
        ICollection<RiskFactor> factors,
        string code,
        string label,
        decimal impact,
        string explanation)
    {
        if (impact <= 0)
        {
            return;
        }

        factors.Add(new RiskFactor(code, label, Math.Round(impact, 1), explanation));
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));
}
