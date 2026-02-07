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

public enum StageProgressOutcome
{
    Null = 0,
    Victory = 1,
    Defeat = 2,
    PromotedOrStayed = 3,
    DemotedOrFailed = 4,
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

public enum TeamNumber
{
    Alpha = 1,
    Bravo = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    [JsonStringEnumMemberName("commander")]
    Commander,

    [JsonStringEnumMemberName("executive_officer")]
    DeputyCommander,

    [JsonStringEnumMemberName("recruitment_officer")]
    Recruiter,

    [JsonStringEnumMemberName("commissioned_officer")]
    CommissionedOfficer,

    [JsonStringEnumMemberName("officer")]
    LineOfficer,

    [JsonStringEnumMemberName("private")]
    Midshipman,
}

public enum Region
{
    Eu,
    Na,
    Asia
}