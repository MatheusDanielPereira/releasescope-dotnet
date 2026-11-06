using ReleaseScope.Domain;

namespace ReleaseScope.Domain.Tests;

public sealed class DeploymentRiskEngineTests
{
    private readonly DeploymentRiskEngine _engine = new();

    [Fact]
    public void Assess_AllSignalsHealthy_AllowsDeployment()
    {
        var signals = new DeploymentSignals(
            ChangedFiles: 4,
            LinesChanged: 80,
            FailedTests: 0,
            TestCoveragePercent: 94m,
            RecentIncidentRate: 0.01m,
            CriticalComponents: 0,
            PerformanceRegressionPercent: 0m,
            SecurityFindings: 0,
            HasRollbackPlan: true);

        var result = _engine.Assess(signals);

        Assert.Equal(RiskLevel.Low, result.Level);
        Assert.Equal(DeploymentDecision.Allow, result.Decision);
        Assert.InRange(result.Score, 0m, 24.9m);
        Assert.Contains(result.Factors, factor => factor.Code == "rollback-plan" && factor.Impact < 0);
    }

    [Fact]
    public void Assess_FailedTestsAndSecurityFindings_BlocksDeployment()
    {
        var signals = new DeploymentSignals(
            ChangedFiles: 44,
            LinesChanged: 2_500,
            FailedTests: 4,
            TestCoveragePercent: 61m,
            RecentIncidentRate: 0.45m,
            CriticalComponents: 2,
            PerformanceRegressionPercent: 22m,
            SecurityFindings: 2,
            HasRollbackPlan: false);

        var result = _engine.Assess(signals);

        Assert.Equal(RiskLevel.Critical, result.Level);
        Assert.Equal(DeploymentDecision.Block, result.Decision);
        Assert.True(result.IsBlocked);
        Assert.Contains(result.Factors, factor => factor.Code == "security-findings");
        Assert.Contains(result.Factors, factor => factor.Code == "failed-tests");
    }

    [Fact]
    public void Assess_RollbackAndCoverage_ReduceRisk()
    {
        var baseSignals = new DeploymentSignals(20, 900, 0, 60m, 0.2m, 1, 8m, 0, false);
        var protectedSignals = baseSignals with
        {
            TestCoveragePercent = 92m,
            HasRollbackPlan = true
        };

        var unprotectedResult = _engine.Assess(baseSignals);
        var protectedResult = _engine.Assess(protectedSignals);

        Assert.True(protectedResult.Score < unprotectedResult.Score);
        Assert.Equal(24m, unprotectedResult.Score - protectedResult.Score);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Assess_NegativeCounts_Throws(int files, int lines, int failures)
    {
        var signals = new DeploymentSignals(files, lines, failures, 80m, 0m, 0, 0m, 0, true);

        Assert.Throws<ArgumentOutOfRangeException>(() => _engine.Assess(signals));
    }

    [Fact]
    public void Assess_FactorsAreOrderedByImpact()
    {
        var signals = new DeploymentSignals(30, 1_800, 2, 85m, 0.3m, 2, 12m, 1, true);

        var result = _engine.Assess(signals);

        Assert.Equal(
            result.Factors.OrderByDescending(factor => factor.Impact),
            result.Factors);
    }
}
