using System.Text.Json.Serialization;

namespace Arona.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StageType
{
    [JsonStringEnumMemberName("promotion")]
    Promotion,

    [JsonStringEnumMemberName("demotion")]
    Demotion,
}

public enum League
{
    Hurricane = 0,
    Typhoon = 1,
    Storm = 2,
    Gale = 3,
    Squall = 4,
}

public enum Division
{
    I = 1,
    II = 2,
    III = 3,
}

public enum Team
{
    Alpha = 1,
    Bravo = 2,
}