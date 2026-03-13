using System.Net.Http.Json;
using Arona.Models;

namespace Arona.Services;

public interface IApiService
{
    public short ServicePort { get; }
    public HttpClient HttpClient { get; set; }
    
    public void SetServicePort(short port);
    public Task<HttpResponseMessage> PostToServiceAsync<TValue>(string endpoint, TValue data);
    public Task<bool> IsServiceOnline();
}

public class ApiService : IApiService
{
    public short ServicePort { get; private set; } = 5242;
    public HttpClient HttpClient { get; set; } = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    public void SetServicePort(short port) =>
        ServicePort = port;

    public async Task<HttpResponseMessage> PostToServiceAsync<TValue>(string endpoint, TValue data) =>
        await HttpClient.PostAsJsonAsync($"http://localhost:{ServicePort}/{endpoint}", data);

    public async Task<bool> IsServiceOnline()
    {
        try
        {
            var pingResponse = await HttpClient.GetAsync($"http://localhost:{ServicePort}/ping");
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

    public static string GetTopLevelDomain(Region region) => region switch
    {
        Region.Eu => "eu",
        Region.Na => "com",
        Region.Asia => "asia",
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
    };
}
