using System.Threading.Channels;

namespace ReleaseScope.Api.Webhooks;

public sealed record QueuedDeploymentEvent(
    string DeliveryId,
    string Repository,
    string Environment,
    string Reference,
    string CommitSha,
    string Actor,
    DateTimeOffset ReceivedAt,
    Domain.DeploymentSignals Signals);

public sealed class DeploymentEventQueue
{
    private readonly Channel<QueuedDeploymentEvent> _channel;

    public DeploymentEventQueue(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _channel = Channel.CreateBounded<QueuedDeploymentEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public ValueTask WriteAsync(QueuedDeploymentEvent deploymentEvent, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(deploymentEvent, cancellationToken);

    public IAsyncEnumerable<QueuedDeploymentEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
