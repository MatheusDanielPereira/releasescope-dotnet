using System.Text.Json;
using ReleaseScope.Domain;

namespace ReleaseScope.Api.Webhooks;

public sealed class GitHubDeploymentEventParser
{
    public bool TryParseDeployment(
        ReadOnlySpan<byte> body,
        string deliveryId,
        DateTimeOffset receivedAt,
        out QueuedDeploymentEvent? deploymentEvent)
    {
        deploymentEvent = null;

        try
        {
            using var document = JsonDocument.Parse(body.ToArray());
            var root = document.RootElement;

            var repository = ReadString(root, "repository", "full_name");
            var deployment = ReadObject(root, "deployment");
            var environment = ReadString(deployment, "environment");
            var reference = ReadString(deployment, "ref");
            var commitSha = ReadString(deployment, "sha");
            var actor = ReadString(root, "sender", "login");

            if (string.IsNullOrWhiteSpace(repository) ||
                string.IsNullOrWhiteSpace(environment) ||
                string.IsNullOrWhiteSpace(reference) ||
                string.IsNullOrWhiteSpace(commitSha))
            {
                return false;
            }

            var signals = ParseSignals(deployment);
            deploymentEvent = new QueuedDeploymentEvent(
                deliveryId,
                Limit(repository, 200),
                Limit(environment, 100),
                Limit(reference, 200),
                Limit(commitSha, 64),
                Limit(actor, 100),
                receivedAt,
                signals);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static DeploymentSignals ParseSignals(JsonElement deployment)
    {
        if (!deployment.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
        {
            return DefaultSignals();
        }

        if (payload.TryGetProperty("releaseScope", out var releaseScope) && releaseScope.ValueKind == JsonValueKind.Object)
        {
            payload = releaseScope;
        }

        return new DeploymentSignals(
            ChangedFiles: ReadInt(payload, "changedFiles", 10, 0, 10_000),
            LinesChanged: ReadInt(payload, "linesChanged", 500, 0, 1_000_000),
            FailedTests: ReadInt(payload, "failedTests", 0, 0, 100_000),
            TestCoveragePercent: ReadDecimal(payload, "testCoveragePercent", 70m, 0m, 100m),
            RecentIncidentRate: ReadDecimal(payload, "recentIncidentRate", 0.05m, 0m, 1m),
            CriticalComponents: ReadInt(payload, "criticalComponents", 1, 0, 1_000),
            PerformanceRegressionPercent: ReadDecimal(payload, "performanceRegressionPercent", 0m, 0m, 10_000m),
            SecurityFindings: ReadInt(payload, "securityFindings", 0, 0, 100_000),
            HasRollbackPlan: ReadBool(payload, "hasRollbackPlan", false));
    }

    private static DeploymentSignals DefaultSignals() => new(
        ChangedFiles: 10,
        LinesChanged: 500,
        FailedTests: 0,
        TestCoveragePercent: 70m,
        RecentIncidentRate: 0.05m,
        CriticalComponents: 1,
        PerformanceRegressionPercent: 0m,
        SecurityFindings: 0,
        HasRollbackPlan: false);

    private static JsonElement ReadObject(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : default;

    private static string ReadString(JsonElement element, params string[] path)
    {
        foreach (var part in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element))
            {
                return string.Empty;
            }
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : string.Empty;
    }

    private static int ReadInt(JsonElement element, string property, int fallback, int minimum, int maximum) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, minimum, maximum)
            : fallback;

    private static decimal ReadDecimal(JsonElement element, string property, decimal fallback, decimal minimum, decimal maximum) =>
        element.TryGetProperty(property, out var value) && value.TryGetDecimal(out var parsed)
            ? Math.Clamp(parsed, minimum, maximum)
            : fallback;

    private static bool ReadBool(JsonElement element, string property, bool fallback) =>
        element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;

    private static string Limit(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}
