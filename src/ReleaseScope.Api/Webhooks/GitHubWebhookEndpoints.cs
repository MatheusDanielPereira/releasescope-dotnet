using Microsoft.Extensions.Options;

namespace ReleaseScope.Api.Webhooks;

public static class GitHubWebhookEndpoints
{
    private const int MaximumBodySize = 1_048_576;

    public static RouteGroupBuilder MapGitHubWebhookEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/webhooks/github", HandleWebhookAsync)
            .WithName("ReceiveGitHubWebhook")
            .DisableAntiforgery();

        group.MapGet("/webhooks/events", (ProcessedWebhookEventStore store) =>
                Results.Ok(store.GetRecent()))
            .WithName("GetProcessedWebhookEvents");

        group.MapGet("/webhooks/events/{deliveryId}", (string deliveryId, ProcessedWebhookEventStore store) =>
                store.Get(deliveryId) is { } processedEvent
                    ? Results.Ok(processedEvent)
                    : Results.NotFound())
            .WithName("GetProcessedWebhookEvent");

        return group;
    }

    private static async Task<IResult> HandleWebhookAsync(
        HttpRequest request,
        IOptions<GitHubWebhookOptions> optionsAccessor,
        GitHubSignatureVerifier signatureVerifier,
        GitHubDeploymentEventParser parser,
        WebhookReplayGuard replayGuard,
        DeploymentEventQueue queue,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Webhook ingestion is not configured");
        }

        if (request.ContentLength is > MaximumBodySize)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var deliveryId = request.Headers["X-GitHub-Delivery"].FirstOrDefault();
        var eventName = request.Headers["X-GitHub-Event"].FirstOrDefault();
        var signature = request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        if (!IsValidDeliveryId(deliveryId) || !string.Equals(eventName, "deployment", StringComparison.Ordinal))
        {
            return Results.BadRequest(new { error = "Unsupported or malformed webhook metadata." });
        }

        await using var bodyBuffer = new MemoryStream();
        await request.Body.CopyToAsync(bodyBuffer, cancellationToken);
        if (bodyBuffer.Length > MaximumBodySize)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var body = bodyBuffer.ToArray();
        if (!signatureVerifier.IsValid(body, signature, options.Secret))
        {
            return Results.Unauthorized();
        }

        var receivedAt = timeProvider.GetUtcNow();
        if (!parser.TryParseDeployment(body, deliveryId!, receivedAt, out var deploymentEvent) || deploymentEvent is null)
        {
            return Results.BadRequest(new { error = "The deployment payload is invalid." });
        }

        var timeToLive = TimeSpan.FromMinutes(Math.Clamp(options.DeliveryTtlMinutes, 1, 10_080));
        if (!replayGuard.TryClaim(deliveryId!, timeToLive))
        {
            return Results.Conflict(new { error = "A webhook with this delivery identifier was already accepted." });
        }

        try
        {
            await queue.WriteAsync(deploymentEvent, cancellationToken);
        }
        catch
        {
            replayGuard.Release(deliveryId!);
            throw;
        }

        return Results.Accepted(
            $"/api/webhooks/events/{Uri.EscapeDataString(deliveryId!)}",
            new { deliveryId, status = "queued" });
    }

    private static bool IsValidDeliveryId(string? deliveryId) =>
        !string.IsNullOrWhiteSpace(deliveryId) &&
        deliveryId.Length <= 128 &&
        deliveryId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
