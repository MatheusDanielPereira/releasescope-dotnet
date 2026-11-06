using ReleaseScope.Api.Webhooks;

namespace ReleaseScope.Api.Tests;

public sealed class WebhookReplayGuardTests
{
    [Fact]
    public void TryClaim_SameDeliveryTwice_RejectsReplay()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.Parse("2026-11-05T12:00:00Z"));
        var guard = new WebhookReplayGuard(timeProvider);

        var firstClaim = guard.TryClaim("delivery-1", TimeSpan.FromMinutes(10));
        var replay = guard.TryClaim("delivery-1", TimeSpan.FromMinutes(10));

        Assert.True(firstClaim);
        Assert.False(replay);
    }

    [Fact]
    public void TryClaim_AfterExpiry_AcceptsDeliveryAgain()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.Parse("2026-11-05T12:00:00Z"));
        var guard = new WebhookReplayGuard(timeProvider);
        guard.TryClaim("delivery-1", TimeSpan.FromMinutes(10));

        timeProvider.Advance(TimeSpan.FromMinutes(11));
        var result = guard.TryClaim("delivery-1", TimeSpan.FromMinutes(10));

        Assert.True(result);
    }

    [Fact]
    public void Release_PreviouslyClaimedDelivery_AllowsRetry()
    {
        var guard = new WebhookReplayGuard(TimeProvider.System);
        guard.TryClaim("delivery-1", TimeSpan.FromMinutes(10));

        guard.Release("delivery-1");

        Assert.True(guard.TryClaim("delivery-1", TimeSpan.FromMinutes(10)));
    }

    private sealed class ManualTimeProvider(DateTimeOffset current) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan duration) => current = current.Add(duration);
    }
}
