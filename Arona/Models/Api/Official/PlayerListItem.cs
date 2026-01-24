using System.Text.Json.Serialization;
using Arona.Utility;

namespace Arona.Models.Api.Official;

internal record PlayerListItemRequest(string Region, string SearchQuery);

internal class PlayerListItemQuery(HttpClient client) : QueryBase<PlayerListItemRequest, ResponseArray<PlayerListItem>>(client)
{
    public override async Task<ResponseArray<PlayerListItem>> GetAsync(PlayerListItemRequest req)
        => await SendAndDeserializeAsync($"https://api.worldofwarships.{req.Region}/wows/clans/list/?application_id={Config.WgApi}&search={req.SearchQuery}");

    public static async Task<ResponseArray<PlayerListItem>> GetSingleAsync(PlayerListItemRequest request) =>
        await new PlayerListItemQuery(ApiClient.Instance).GetAsync(request);
}

/// <summary>
/// Response deserialization model for API endpoint /wows/account/list/ which returns a list of players based on a name search.
/// </summary>
internal class PlayerListItem
{
    [JsonPropertyName("account_id")]
    public required long AccountId { get; set; }

    [JsonPropertyName("nickname")]
    public required string Nickname { get; set; }
}