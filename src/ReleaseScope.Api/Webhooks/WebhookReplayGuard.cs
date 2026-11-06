using System.Collections.Concurrent;

namespace ReleaseScope.Api.Webhooks;

public sealed class WebhookReplayGuard(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _claimedDeliveries = new(StringComparer.Ordinal);

    public bool TryClaim(string deliveryId, TimeSpan timeToLive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(timeToLive);

        while (true)
        {
            if (_claimedDeliveries.TryAdd(deliveryId, expiresAt))
            {
                RemoveExpiredEntries(now);
                return true;
            }

            if (!_claimedDeliveries.TryGetValue(deliveryId, out var currentExpiry) || currentExpiry > now)
            {
                return false;
            }

            if (_claimedDeliveries.TryUpdate(deliveryId, expiresAt, currentExpiry))
            {
                return true;
            }
        }
    }

    public void Release(string deliveryId) => _claimedDeliveries.TryRemove(deliveryId, out _);

    private void RemoveExpiredEntries(DateTimeOffset now)
    {
        if (_claimedDeliveries.Count < 512)
        {
            return;
        }

        foreach (var entry in _claimedDeliveries)
        {
            if (entry.Value <= now)
            {
                _claimedDeliveries.TryRemove(entry.Key, out _);
            }
        }
    }
}
