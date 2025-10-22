using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.ApiModels;

internal class AccountInfoSync
{
    [JsonPropertyName("id")]
    public required long AccountId { get; set; }

    [JsonPropertyName("clan_id")]
    public int ClanId { get; set; }

    public static async Task<AccountInfoSync> GetAsync(string cookie, string region)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Cookie", $"wsauth_token={cookie};");

        var res = await client.GetAsync($"https://clans.worldofwarships.{region}/account_info_sync/");
        if (res.StatusCode == HttpStatusCode.Forbidden)
            throw new InvalidCredentialException("Invalid or expired cookie.");
        res.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<AccountInfoSync>(await res.Content.ReadAsStringAsync())!;
    }
}