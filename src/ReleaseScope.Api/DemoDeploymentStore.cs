using ReleaseScope.Domain;

namespace ReleaseScope.Api;

public sealed record DeploymentView(
    Guid Id,
    string Service,
    string Version,
    string Environment,
    string Commit,
    string Author,
    DateTimeOffset StartedAt,
    string Status,
    RiskAssessment Risk);

public sealed record DashboardView(
    int DeploymentsToday,
    int BlockedDeployments,
    decimal AverageRisk,
    decimal ChangeFailureRate,
    IReadOnlyList<DeploymentView> Deployments);

public sealed class DemoDeploymentStore
{
    private readonly IReadOnlyList<DeploymentView> _deployments;

    public DemoDeploymentStore(DeploymentRiskEngine engine)
    {
        var now = DateTimeOffset.UtcNow;
        _deployments =
        [
            Create(engine, "identity-api", "v2.18.0", "Production", "8fa32c1", "Maya Chen", now.AddMinutes(-8),
                new DeploymentSignals(8, 214, 0, 92m, 0.04m, 1, 1.8m, 0, true)),
            Create(engine, "billing-worker", "v5.7.3", "Production", "1bc90de", "Liam Brooks", now.AddMinutes(-27),
                new DeploymentSignals(34, 1860, 2, 71m, 0.31m, 2, 18m, 1, false)),
            Create(engine, "customer-portal", "v3.11.2", "Staging", "e32bd77", "Sofia Alvarez", now.AddHours(-1),
                new DeploymentSignals(16, 640, 0, 86m, 0.08m, 0, 4m, 0, true)),
            Create(engine, "notification-hub", "v1.26.0", "Production", "c7d20a9", "Noah Williams", now.AddHours(-2),
                new DeploymentSignals(21, 980, 0, 78m, 0.14m, 1, 9m, 0, true)),
            Create(engine, "reporting-api", "v4.3.1", "Production", "42d8ef0", "Aisha Khan", now.AddHours(-4),
                new DeploymentSignals(4, 92, 0, 95m, 0.02m, 0, 0m, 0, true))
        ];
    }

    public IReadOnlyList<DeploymentView> GetAll() => _deployments;

    public DashboardView GetDashboard()
    {
        var blocked = _deployments.Count(item => item.Risk.IsBlocked);
        return new DashboardView(
            _deployments.Count,
            blocked,
            Math.Round(_deployments.Average(item => item.Risk.Score), 1),
            Math.Round(blocked / (decimal)_deployments.Count * 100m, 1),
            _deployments);
    }

    private static DeploymentView Create(
        DeploymentRiskEngine engine,
        string service,
        string version,
        string environment,
        string commit,
        string author,
        DateTimeOffset startedAt,
        DeploymentSignals signals)
    {
        var risk = engine.Assess(signals);
        var status = risk.Decision switch
        {
            DeploymentDecision.Allow => "Deploying",
            DeploymentDecision.ReviewRequired => "Needs review",
            _ => "Blocked"
        };

        return new DeploymentView(
            Guid.NewGuid(), service, version, environment, commit, author, startedAt, status, risk);
    }
}
