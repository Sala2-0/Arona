using Arona.Models;

namespace Arona.Utility;

public static class ClanUtils
{
    public static string GetPromotionType(StageType type) => type switch
    {
        StageType.Promotion => "Q+",
        StageType.Demotion => "Q-",
        _ => throw new ArgumentException("Invalid stage type", nameof(type))
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
    /// Unix time in seconds of the end of the current clan battle session.
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

            // EU has special logic cause of CET/CEST
            case 4 or 5 or 6 or 7:
            {
                var euTime = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                var localEnd = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 23, 30, 0, DateTimeKind.Unspecified);

                endSession = TimeZoneInfo.ConvertTimeToUtc(localEnd, euTime);
                break;
            }
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

    
    /// <summary>
    /// Gets the color of a clan's league.
    /// </summary>
    /// <param name="league">
    /// League enum value.
    /// </param>
    /// <returns></returns>
    public static string GetLeagueColor(League league) => league switch
    {
        League.Hurricane => "cda4ff",   // Hurricane
        League.Typhoon => "bee7bd",     // Typhoon
        League.Storm => "e3d6a0",       // Storm
        League.Gale => "cce4e4",        // Gale
        League.Squall => "cc9966",      // Squall
        _ => "ffffff"                   // Undefined
    };

    /// <summary>
    /// Converts a top-level domain name to a human-readable format.
    /// </summary>
    /// <param name="region">
    /// Top-level domain name.
    /// </param>
    public static string GetHumanRegion(string region) => region switch
    {
        "eu" or "EU" => "EU",
        "asia" or "ASIA" => "ASIA",
        "com" => "NA",
        _ => "undefined"
    };
    
    /// <summary>
    /// Converts a region string to a region code returned/used by clans.worldofwarships subdomain.
    /// </summary>
    /// <param name="region">
    /// Top-level domain name.
    /// </param>
    public static string ConvertRegion(string region) => region switch
    {
        "eu" or "EU" => "eu",
        "com" or "COM" => "us",
        "asia" or "ASIA" => "sg",
        _ => throw new ArgumentException("Invalid region", nameof(region))
    };

    /// <summary>
    /// Converts region code back to a region string.
    /// </summary>
    /// <param name="realm">
    /// Region code returned/used by clans.worldofwarships subdomain.
    /// </param>
    public static string ConvertRealm(string realm) => realm switch
    {
        "global" => "Global",
        "eu" => "EU",
        "sg" => "ASIA",
        "us" => "NA",
        _ => throw new ArgumentException("Invalid realm", nameof(realm))
    };
}