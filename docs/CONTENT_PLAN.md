# ReleaseScope build-in-public series

The content is designed to teach something useful in every post. Project promotion is secondary to the engineering lesson.

## Post 1 — The decision

**Hook:** Would you deploy this change?

Show two deployment cards with similar change sizes but different operational signals. Ask readers which one should be blocked before revealing the model.

## Post 2 — Explainable risk

**Hook:** A risk score without an explanation is just another magic number.

Explain why every score is decomposed into factors and why protective controls produce negative impact.

## Post 3 — Architecture

**Hook:** I chose asynchronous analysis because deployment pipelines should not wait for slow integrations.

Publish the event-flow diagram and discuss delivery guarantees, idempotency, and failure modes.

## Post 4 — Security

**Hook:** A valid webhook can still be a replay attack.

Demonstrate signature verification, timestamp tolerance, replay protection, rate limiting, and audit events.

## Post 5 — Observability

**Hook:** One deployment. Four services. One trace.

Show the trace from webhook ingestion to policy decision and notification.

## Post 6 — Failure injection

**Hook:** I intentionally slowed a dependency by 800 ms. Here is what the system saw.

Use a short video to show latency regression, the risk score change, and the deployment decision.

## Post 7 — Performance

**Hook:** The first version processed X events/second. Then the profiler changed my design.

Publish reproducible before-and-after results with the exact test setup.

## Post 8 — Release

Share the repository, one-command local setup, automated test count, architecture, and lessons learned. End with a specific technical question rather than a generic engagement request.
