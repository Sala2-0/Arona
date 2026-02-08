using System.Text.Json;
using Arona.Utility;

namespace Arona.Models.Api;

public abstract class QueryBase<TRequest, TResponse>(HttpClient client) : IApiQuery<TRequest, TResponse>
{
    protected readonly HttpClient Client = client;

    public abstract Task<TResponse> GetAsync(TRequest request);

    protected async Task<TResponse> SendAndDeserializeAsync(string url, HttpRequestMessage? message = null)
    {
        HttpResponseMessage res;

        if (message != null)
            res = await Client.SendAsync(message);
        else
            res = await Client.GetAsync(url);

        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TResponse>(json, Converter.Options)
               ?? throw new JsonException("Failed to deserialize API response");
    }
}
