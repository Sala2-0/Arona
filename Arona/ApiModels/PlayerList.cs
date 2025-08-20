using System.Text.Json.Serialization;

namespace Arona.ApiModels;

internal class PlayerList
{
    [JsonPropertyName("nickname")] public required string Nickname { get; init; }
    [JsonPropertyName("account_id")] public required long AccountId { get; init; }
}