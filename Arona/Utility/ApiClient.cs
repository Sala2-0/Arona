namespace Arona.Utility;

internal static class ApiClient
{
    public static readonly HttpClient Instance = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });
}
