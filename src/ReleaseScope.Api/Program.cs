using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using ReleaseScope.Api;
using ReleaseScope.Api.Webhooks;
using ReleaseScope.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<DeploymentRiskEngine>();
builder.Services.AddSingleton<DemoDeploymentStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<GitHubWebhookOptions>(
    builder.Configuration.GetSection(GitHubWebhookOptions.SectionName));
builder.Services.AddSingleton<GitHubSignatureVerifier>();
builder.Services.AddSingleton<GitHubDeploymentEventParser>();
builder.Services.AddSingleton<WebhookReplayGuard>();
builder.Services.AddSingleton<ProcessedWebhookEventStore>();
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var configuredCapacity = configuration.GetValue<int?>("GitHubWebhook:QueueCapacity") ?? 256;
    return new DeploymentEventQueue(Math.Clamp(configuredCapacity, 16, 10_000));
});
builder.Services.AddHostedService<DeploymentEventWorker>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

var api = app.MapGroup("/api")
    .WithTags("ReleaseScope");

api.MapGet("/deployments", (DemoDeploymentStore store) => Results.Ok(store.GetAll()))
    .WithName("GetDeployments");

api.MapGet("/dashboard", (DemoDeploymentStore store) => Results.Ok(store.GetDashboard()))
    .WithName("GetDashboard");

api.MapPost("/assessments", (DeploymentSignals signals, DeploymentRiskEngine engine) =>
        Results.Ok(engine.Assess(signals)))
    .WithName("AssessDeployment");

api.MapGitHubWebhookEndpoints();

app.Run();

public partial class Program;
