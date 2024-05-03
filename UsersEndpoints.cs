namespace GitHub.Api;

public static class UsersEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/{username}", async (
            string username,
            GitHubService gitHubService,
            CancellationToken cancellationToken) =>
        {
            var user = await gitHubService.GetByUsernameAsync(username, cancellationToken);

            return Results.Ok(user);
        });
    }
}