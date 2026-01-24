using System.Text.Json.Serialization;
using Arona.Utility;

namespace Arona.Models.Api.Official;

internal record ClanListItemRequest(string Region, string SearchQuery);

internal class ClanListItemQuery(HttpClient client) : QueryBase<ClanListItemRequest, ResponseArray<ClanListItem>>(client)
{
    public override async Task<ResponseArray<ClanListItem>> GetAsync(ClanListItemRequest req)
        => await SendAndDeserializeAsync($"https://api.worldofwarships.{req.Region}/wows/clans/list/?application_id={Config.WgApi}&search={req.SearchQuery}");

    public static async Task<ResponseArray<ClanListItem>> GetSingleAsync(ClanListItemRequest request)
    {
        var apiQuery = new ClanListItemQuery(ApiClient.Instance);
        return await apiQuery.GetAsync(request);
    }
}

/// <summary>
/// Response deserialization model for API endpoint /wows/clans/list/ which returns a list of clans based on a search query.
/// </summary>
internal class ClanListItem
{
    /// <summary>
    /// Total number of members in the clan.
    /// </summary>
    [JsonPropertyName("members_count")]
    public required int MemberCount { get; set; }

    /// <summary>
    /// Clan creation date as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required long CreatedAt { get; set; }

    [JsonPropertyName("clan_id")]
    public required int ClanId { get; set; }

    [JsonPropertyName("tag")]
    public required string Tag { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}