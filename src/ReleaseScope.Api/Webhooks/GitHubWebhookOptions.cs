namespace ReleaseScope.Api.Webhooks;

public sealed class GitHubWebhookOptions
{
    public const string SectionName = "GitHubWebhook";

    public string Secret { get; init; } = string.Empty;

    public int DeliveryTtlMinutes { get; init; } = 1_440;

    public int QueueCapacity { get; init; } = 256;
}
