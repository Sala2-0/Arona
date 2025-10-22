using System.Text.Json.Serialization;

namespace Arona.Models.Api.Official;

/// <summary>
/// Used to deserialize responses from Official API and only extract what we need.
/// </summary>
/// <remarks>This version is for data that deserializes to an array</remarks>
/// <typeparam name="T">
/// Property that [JsonPropertyName("data")] deserializes to.
/// </typeparam>
public class ResponseArray<T>
{
    [JsonPropertyName("data")]
    public required T[] Data { get; set; }
}

/// <summary>
/// Used to deserialize responses from Official API and only extract what we need.
/// </summary>
/// <remarks>This version is for data that deserializes to an object</remarks>
/// <typeparam name="T">
/// Property that [JsonPropertyName("data")] deserializes to.
/// </typeparam>
public class ResponseObject<T>
{
    [JsonPropertyName("data")]
    public required Dictionary<string, T> Data { get; set; }
}