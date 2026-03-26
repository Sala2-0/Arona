using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.Models.Api.Clans;

public record AccountInfoSyncRequest(string Cookie, string Region);

public class AccountInfoSyncQuery(HttpClient client) : QueryBase<AccountInfoSyncRequest, AccountInfoSync>(client)
{
    public override async Task<AccountInfoSync> GetAsync(AccountInfoSyncRequest request)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request.Region);
        httpRequestMessage.Headers.Add("Cookie", $"wsauth_token={request.Region};");
        
        return await SendAndDeserializeAsync($"https://clans.worldofwarships.{request.Region}/account_info_sync/", httpRequestMessage);
    }
}

public class AccountInfoSync
{
    [JsonPropertyName("id")]
    public required long AccountId { get; set; }

    [JsonPropertyName("clan_id")]
    public int? ClanId { get; set; }

    [JsonPropertyName("role_name")]
    public Role? Rank { get; set; }
}