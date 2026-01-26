namespace Arona.Utility;

public static class ApiClient
{
    public static readonly HttpClient Instance = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });
}
