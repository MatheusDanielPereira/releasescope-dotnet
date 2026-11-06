namespace ReleaseScope.Domain;

public enum RiskLevel
{
    Low,
    Moderate,
    High,
    Critical
}

public enum DeploymentDecision
{
    Allow,
    ReviewRequired,
    Block
}

public sealed record DeploymentSignals(
    int ChangedFiles,
    int LinesChanged,
    int FailedTests,
    decimal TestCoveragePercent,
    decimal RecentIncidentRate,
    int CriticalComponents,
    decimal PerformanceRegressionPercent,
    int SecurityFindings,
    bool HasRollbackPlan);

public sealed record RiskFactor(
    string Code,
    string Label,
    decimal Impact,
    string Explanation);

public sealed record RiskAssessment(
    decimal Score,
    RiskLevel Level,
    DeploymentDecision Decision,
    IReadOnlyList<RiskFactor> Factors)
{
    public bool IsBlocked => Decision is DeploymentDecision.Block;
}
