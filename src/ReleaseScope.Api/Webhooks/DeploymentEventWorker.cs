using ReleaseScope.Domain;

namespace ReleaseScope.Api.Webhooks;

public sealed class DeploymentEventWorker(
    DeploymentEventQueue queue,
    DeploymentRiskEngine riskEngine,
    ProcessedWebhookEventStore eventStore,
    TimeProvider timeProvider,
    ILogger<DeploymentEventWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var deploymentEvent in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                var risk = riskEngine.Assess(deploymentEvent.Signals);
                eventStore.Add(new ProcessedWebhookEvent(
                    deploymentEvent.DeliveryId,
                    deploymentEvent.Repository,
                    deploymentEvent.Environment,
                    deploymentEvent.Reference,
                    deploymentEvent.CommitSha,
                    deploymentEvent.Actor,
                    deploymentEvent.ReceivedAt,
                    timeProvider.GetUtcNow(),
                    risk));

                logger.LogInformation(
                    "Processed deployment webhook with decision {Decision} and risk level {RiskLevel}",
                    risk.Decision,
                    risk.Level);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process a queued deployment webhook");
            }
        }
    }
}
