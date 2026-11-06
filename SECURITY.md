# Security policy

ReleaseScope is a reference project and must not be connected to production deployment systems without a security review.

Please report vulnerabilities privately through GitHub Security Advisories. Do not include credentials, access tokens, customer data, or private repository information in an issue.

The demo uses generated data only. No employer source code, schemas, credentials, or proprietary business rules are included.

## Webhook security controls

- GitHub deployment payloads require an `X-Hub-Signature-256` HMAC-SHA256 signature.
- Signatures are compared in constant time to reduce timing side channels.
- `X-GitHub-Delivery` identifiers are validated and claimed for a configurable TTL to reject replayed requests.
- Request bodies are limited to 1 MB and only the `deployment` event is accepted.
- Accepted events enter a bounded in-memory queue so bursts cannot create unbounded memory growth.
- Webhook bodies, signatures, secrets, and repository details are not written to application logs.

Configure `GitHubWebhook__Secret` through an environment variable or a secure secret provider. Never commit the webhook secret to this repository.

The replay guard, queue, and recent-event store are currently process-local. Restarting the API clears their state. A durable broker and persistent idempotency store are required before production use across multiple instances.
