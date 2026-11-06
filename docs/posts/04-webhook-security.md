# A valid webhook can still be a replay attack

Signing a webhook proves who created the payload. It does not prove the payload is new.

That distinction shaped the latest ReleaseScope milestone: secure ingestion of GitHub deployment events before they reach the deployment risk engine.

The path now looks like this:

`GitHub -> HMAC validation -> replay guard -> bounded queue -> background worker -> explainable risk decision`

The controls are intentionally small and reviewable:

- HMAC-SHA256 validation over the exact raw body.
- Constant-time signature comparison.
- A unique delivery ID claimed for a configurable TTL.
- A 1 MB request limit and strict acceptance of deployment events only.
- A bounded queue to prevent unbounded memory growth during bursts.
- No payloads, signatures, secrets, or repository details in logs.

I also tested the behavior through the real HTTP boundary: the first signed delivery returned `202 Accepted`, the worker produced a high-risk review decision, and the same delivery ID returned `409 Conflict` on replay.

The important limitation is explicit: replay claims, queued events, and recent results are still process-local. The next milestone is durable messaging and persistent idempotency for multi-instance execution.

When you design webhook consumers, where do you draw the boundary between signature validation, idempotency, and durable processing?

#dotnet #softwarearchitecture #cybersecurity #github #backenddevelopment
