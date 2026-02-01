using System.Net.Http.Json;

namespace Arona.Utility;

public static class ApiClient
{
    private static short _servicePort = 5242;
    public static readonly HttpClient Instance = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    public static void SetServicePort(short port) =>
        _servicePort = port;

    public static async Task<HttpResponseMessage> PostToServiceAsync<TValue>(string endpoint, TValue data) =>
        await Instance.PostAsJsonAsync($"http://localhost:{_servicePort}/{endpoint}", data);
}
