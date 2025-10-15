using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Arona.Utility;

public static class ClanUtils
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StageType
    {
        [EnumMember(Value = "promotion")] Promotion,
        [EnumMember(Value = "Demotion")] Demotion,
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

    public static string GetPromotionType(StageType type) => type switch
    {
        StageType.Promotion => "Q+",
        StageType.Demotion => "Q-",
    };

    public static double GetLeagueExponent(League league) => league switch
    {
        League.Hurricane => 1.0,    // Hurricane
        League.Typhoon => 0.8,      // Typhoon
        League.Storm => 0.6,        // Storm
        League.Gale => 0.4,         // Gale
        League.Squall => 0.2,       // Squall
        _ => 0                      // Undefined
    };

    /// <summary>
    /// Gets the end time of the current clan battle session.
    /// </summary>
    /// <param name="primeTime">
    /// Integer value representing day and region
    /// </param>
    /// <returns>
    /// Unix time seconds of the end of the current clan battle session.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Where <paramref name="primeTime"/> is not between 0 and 11.
    /// </exception>
    public static long? GetEndSession(int? primeTime)
    {
        if (primeTime is null) return null;

        var utcNow = DateTime.UtcNow;

        DateTime endSession;
        switch (primeTime)
        {
            case 0 or 1 or 2 or 3:
                endSession = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 16, 0, 0, DateTimeKind.Utc);
                break;
            case 4 or 5 or 6 or 7:
                endSession = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 21, 30, 0, DateTimeKind.Utc);
                break;
            case 8 or 9 or 10 or 11:
                endSession = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 4, 0, 0, DateTimeKind.Utc);
                if (utcNow.Hour >= 4) endSession = endSession.AddDays(1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(primeTime), "Unexpected region value");
        }

        if (utcNow >= endSession) return null;
        return ((DateTimeOffset)endSession).ToUnixTimeSeconds();
    }

    public static string GetLeagueColor(League league) => league switch
    {
        League.Hurricane => "cda4ff", // Hurricane
        League.Typhoon => "bee7bd", // Typhoon
        League.Storm => "e3d6a0", // Storm
        League.Gale => "cce4e4", // Gale
        League.Squall => "cc9966", // Squall
        _ => "ffffff"  // Undefined
    };

    public static string GetRegionCode(string region) => region switch
    {
        "eu" or "EU" => "EU",
        "asia" or "ASIA" => "ASIA",
        "com" => "NA",
        _ => "undefined"
    };
}