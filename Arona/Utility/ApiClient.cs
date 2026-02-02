using System.Net.Http.Json;

namespace Arona.Utility;

public static class ApiClient
{
    public static short ServicePort { get; private set; } = 5242;
    public static readonly HttpClient Instance = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    public static void SetServicePort(short port) =>
        ServicePort = port;

    public static async Task<HttpResponseMessage> PostToServiceAsync<TValue>(string endpoint, TValue data) =>
        await Instance.PostAsJsonAsync($"http://localhost:{ServicePort}/{endpoint}", data);

    public static async Task<bool> IsServiceOnline()
    {
        try
        {
            var pingResponse = await Instance.GetAsync($"http://localhost:{ApiClient.ServicePort}/ping");
            pingResponse.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not reach service API on port " + ServicePort);
            Console.WriteLine("Make sure to start the service and ensure correct port is set");
            Console.WriteLine(ex.Message);

            return false;
        }
    }
}
