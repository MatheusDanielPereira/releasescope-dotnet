# GitHub deployment webhooks

ReleaseScope accepts signed GitHub `deployment` events, rejects replayed deliveries, and assesses accepted events asynchronously.

## Configure locally

Set a random secret outside source control, then start the API:

```powershell
$env:GitHubWebhook__Secret = "replace-with-a-random-webhook-secret"
dotnet run --project src/ReleaseScope.Api
```

In the GitHub webhook configuration, use:

- Payload URL: `https://your-host/api/webhooks/github`
- Content type: `application/json`
- Secret: the same value supplied to the API
- Event: `deployment`

## Request contract

Required headers:

- `X-GitHub-Event: deployment`
- `X-GitHub-Delivery`: unique letters, digits, hyphens, or underscores; maximum 128 characters
- `X-Hub-Signature-256`: `sha256=` followed by the HMAC-SHA256 of the exact raw request body

The standard GitHub payload supplies repository, environment, ref, SHA, and actor. Optional risk signals can be included under `deployment.payload.releaseScope`:

```json
{
  "repository": { "full_name": "portfolio/releasescope" },
  "sender": { "login": "octocat" },
  "deployment": {
    "environment": "production",
    "ref": "main",
    "sha": "0123456789abcdef0123456789abcdef01234567",
    "payload": {
      "releaseScope": {
        "changedFiles": 42,
        "linesChanged": 2800,
        "failedTests": 0,
        "testCoveragePercent": 78,
        "recentIncidentRate": 0.08,
        "criticalComponents": 2,
        "performanceRegressionPercent": 3,
        "securityFindings": 0,
        "hasRollbackPlan": true
      }
    }
  }
}
```

When optional signals are absent, the parser applies conservative demo defaults. Numeric inputs are clamped to accepted ranges before risk assessment.

## Responses

- `202 Accepted`: signature and payload are valid; the event is queued.
- `400 Bad Request`: malformed metadata or deployment payload.
- `401 Unauthorized`: missing or invalid signature.
- `409 Conflict`: the delivery identifier was already accepted within the replay window.
- `413 Payload Too Large`: request exceeds 1 MB.
- `503 Service Unavailable`: no webhook secret is configured.

The `Location` returned with `202` points to `/api/webhooks/events/{deliveryId}`. Processing is asynchronous, so that resource can briefly return `404` until the worker finishes.

## Current durability boundary

The queue, replay claims, and last 200 processed events are in memory. They make the security behavior easy to inspect in this portfolio milestone but do not survive restarts or coordinate multiple API instances. Production hardening requires a durable broker and a persistent idempotency store.
