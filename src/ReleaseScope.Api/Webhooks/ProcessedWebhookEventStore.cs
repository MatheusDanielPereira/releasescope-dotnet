using System.Collections.Concurrent;
using ReleaseScope.Domain;

namespace ReleaseScope.Api.Webhooks;

public sealed record ProcessedWebhookEvent(
    string DeliveryId,
    string Repository,
    string Environment,
    string Reference,
    string CommitSha,
    string Actor,
    DateTimeOffset ReceivedAt,
    DateTimeOffset ProcessedAt,
    RiskAssessment Risk);

public sealed class ProcessedWebhookEventStore
{
    private const int MaximumItems = 200;
    private readonly ConcurrentDictionary<string, ProcessedWebhookEvent> _events = new(StringComparer.Ordinal);

    public void Add(ProcessedWebhookEvent processedEvent)
    {
        _events[processedEvent.DeliveryId] = processedEvent;

        if (_events.Count <= MaximumItems)
        {
            return;
        }

        foreach (var item in _events.Values.OrderBy(item => item.ProcessedAt).Take(_events.Count - MaximumItems))
        {
            _events.TryRemove(item.DeliveryId, out _);
        }
    }

    public ProcessedWebhookEvent? Get(string deliveryId) =>
        _events.GetValueOrDefault(deliveryId);

    public IReadOnlyList<ProcessedWebhookEvent> GetRecent() =>
        _events.Values
            .OrderByDescending(item => item.ProcessedAt)
            .ToArray();
}
