using System.Text.Json.Serialization;

namespace Arona.ApiModels;

public class VehicleInfo
{
    [JsonPropertyName("cd")] public required long Id { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("short_name")] public required string ShortName { get; init; }
    [JsonPropertyName("level")] public required int Tier { get; init; }
}