namespace GitHub.Api;

public sealed class GitHubService
{
    private readonly HttpClient _client;
    private static readonly Random Random = new();

    public GitHubService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GitHubUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var content = await _client.GetFromJsonAsync<GitHubUser>($"users/{username}", cancellationToken);

        return content;
    }
}
