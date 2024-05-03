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

var httpClientBuilder = builder.Services.AddHttpClient<GitHubService>((sp, httpClient) =>
{
    var gitHubSettings = sp.GetRequiredService<IOptions<GitHubSettings>>().Value;

    httpClient.DefaultRequestHeaders.Add("Authorization", gitHubSettings.AccessToken);
    httpClient.DefaultRequestHeaders.Add("User-Agent", gitHubSettings.UserAgent);

    httpClient.BaseAddress = new Uri(gitHubSettings.BaseAddress);
});

// Add Chaos
httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 30%
    const double InjectionRate = 0.3;

    _ = builder
        // Add latency to simulate network delays
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(1))
        // Simulate server errors
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
        ;
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
