using System.Text;
using ReleaseScope.Api.Webhooks;

namespace ReleaseScope.Api.Tests;

public sealed class GitHubDeploymentEventParserTests
{
    private readonly GitHubDeploymentEventParser _parser = new();

    [Fact]
    public void TryParse_ValidDeployment_MapsMetadataAndSignals()
    {
        const string json = """
            {
              "repository": { "full_name": "example/releasescope" },
              "sender": { "login": "octocat" },
              "deployment": {
                "environment": "production",
                "ref": "refs/heads/main",
                "sha": "0123456789abcdef",
                "payload": {
                  "releaseScope": {
                    "changedFiles": 24,
                    "linesChanged": 940,
                    "failedTests": 2,
                    "testCoveragePercent": 84.5,
                    "recentIncidentRate": 0.2,
                    "criticalComponents": 2,
                    "performanceRegressionPercent": 12,
                    "securityFindings": 1,
                    "hasRollbackPlan": true
                  }
                }
              }
            }
            """;

        var result = _parser.TryParseDeployment(
            Encoding.UTF8.GetBytes(json),
            "delivery-42",
            DateTimeOffset.Parse("2026-11-05T12:00:00Z"),
            out var deploymentEvent);

        Assert.True(result);
        Assert.NotNull(deploymentEvent);
        Assert.Equal("example/releasescope", deploymentEvent.Repository);
        Assert.Equal("production", deploymentEvent.Environment);
        Assert.Equal(24, deploymentEvent.Signals.ChangedFiles);
        Assert.Equal(84.5m, deploymentEvent.Signals.TestCoveragePercent);
        Assert.True(deploymentEvent.Signals.HasRollbackPlan);
    }

    [Fact]
    public void TryParse_MissingOptionalPayload_UsesConservativeDefaults()
    {
        const string json = """
            {
              "repository": { "full_name": "example/releasescope" },
              "deployment": {
                "environment": "staging",
                "ref": "main",
                "sha": "0123456789abcdef"
              }
            }
            """;

        var result = _parser.TryParseDeployment(
            Encoding.UTF8.GetBytes(json),
            "delivery-43",
            DateTimeOffset.UtcNow,
            out var deploymentEvent);

        Assert.True(result);
        Assert.NotNull(deploymentEvent);
        Assert.Equal(10, deploymentEvent.Signals.ChangedFiles);
        Assert.False(deploymentEvent.Signals.HasRollbackPlan);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not-json")]
    [InlineData("{\"repository\":{\"full_name\":\"example/repo\"}}")]
    public void TryParse_InvalidPayload_ReturnsFalse(string json)
    {
        var result = _parser.TryParseDeployment(
            Encoding.UTF8.GetBytes(json),
            "delivery-invalid",
            DateTimeOffset.UtcNow,
            out var deploymentEvent);

        Assert.False(result);
        Assert.Null(deploymentEvent);
    }
}
