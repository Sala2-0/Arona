using System.Text.Json.Serialization;
using Arona.Utility;

namespace Arona.Models.Api.Clans;

internal class VehicleInfo
{
    [JsonPropertyName("cd")]
    public required long Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("short_name")] 
    public required string ShortName { get; init; }
    
    [JsonPropertyName("level")]
    public required Text.Tier Tier { get; init; }
}