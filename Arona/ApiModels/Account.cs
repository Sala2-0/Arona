using System.Text.Json.Serialization;
using Arona.Database;

namespace Arona.ApiModels;

internal class Account
{
    [JsonPropertyName("statistics")] public Dictionary<long, ShipStatistics> ShipStats { get; set; }
}

internal class ShipStatistics
{
    [JsonExtensionData] public Dictionary<Mode, object> Modes { get; set; }
}