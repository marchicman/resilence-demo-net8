using GitHub.Api;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Simmy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<GitHubSettings>()
    .BindConfiguration(GitHubSettings.ConfigurationSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CustomPipelineSettings>()
    .BindConfiguration(CustomPipelineSettings.ConfigurationSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var httpClientBuilder = builder.Services.AddHttpClient<GitHubService>((sp, httpClient) =>
{
    var gitHubSettings = sp.GetRequiredService<IOptions<GitHubSettings>>().Value;

    httpClient.DefaultRequestHeaders.Add("Authorization", gitHubSettings.AccessToken);
    httpClient.DefaultRequestHeaders.Add("User-Agent", gitHubSettings.UserAgent);

    httpClient.BaseAddress = new Uri(gitHubSettings.BaseAddress);
});

// Step 1 Add StandardResilienceHandler
/*
 * RateLimiter(httpStandardResilienceOptions.RateLimiter)
 * Timeout(httpStandardResilienceOptions.TotalRequestTimeout)
 * Retry(httpStandardResilienceOptions.Retry)
 * CircuitBreaker(httpStandardResilienceOptions.CircuitBreaker)
 * Timeout(httpStandardResilienceOptions.AttemptTimeout);
 */
httpClientBuilder.AddStandardResilienceHandler()
  .Configure((options, serviceProvider) => // Step 2 show configuration
    {
        // Step 3 Use config to setup timeout
        var customPipelineSettings = serviceProvider.GetRequiredService<IOptions<CustomPipelineSettings>>().Value;
        
        options.AttemptTimeout = customPipelineSettings.Timeout;
        options.AttemptTimeout.OnTimeout = (args) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>(); // N.B: Dependency Injection is native 
            logger.LogWarning("Attempt timeout!");
            return default;
        };
        
        // Update circuit breaker to handle transient errors and InvalidOperationException
        options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };

        // Default Value MaxRetryAttempts is 3
        options.Retry.MaxRetryAttempts = 5;

        // Update retry strategy to handle transient errors and InvalidOperationException
        options.Retry.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };
    });

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{

    _ = builder
        .AddChaosLatency(0.2, TimeSpan.FromSeconds(10)) // Add latency to simulate network delays
        .AddChaosFault(0.3, () => new InvalidOperationException("Chaos strategy injection!")) // Inject faults to simulate system errors
        .AddChaosOutcome(0.3, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.MapUserEndpoints();

app.Run();
