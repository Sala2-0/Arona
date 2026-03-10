using System.Net.Http.Json;
using Arona.Models;

namespace Arona.Services;

public interface IApiClient
{
    HttpClient HttpClient { get; }
    short ServicePort { get; }
    void SetServicePort(short port);
    Task<bool> IsServiceOnlineAsync();
    Task<HttpResponseMessage> PostToServiceAsync<TValue>(string endpoint, TValue data);
}

public class ApiClient(HttpClient client) : IApiClient
{
    public HttpClient HttpClient { get; } = client;
    public short ServicePort { get; private set; } = 5242;

    public void SetServicePort(short port) => ServicePort = port;

    public async Task<HttpResponseMessage> PostToServiceAsync<TValue>(string endpoint, TValue data) =>
        await HttpClient.PostAsJsonAsync($"http://localhost:{ServicePort}/{endpoint}", data);

    public async Task<bool> IsServiceOnlineAsync()
    {
        try
        {
            var response = await HttpClient.GetAsync($"http://localhost:{ServicePort}/ping");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not reach service API on port " + ServicePort);
            Console.WriteLine("Make sure to start the service and ensure correct port is set");
            Console.WriteLine(ex);
            return false;
        }
    }

    // Statiska hjälpmetoder kan stanna som statiska om de är "rena" (pure)
    public static string GetTopLevelDomain(Region region) => region switch
    {
        Region.Eu => "eu",
        Region.Na => "com",
        Region.Asia => "asia",
        _ => throw new ArgumentOutOfRangeException(nameof(region))
    };
}
