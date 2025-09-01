namespace Arona.Utility;

internal class ClanSearchStructure(string clanTag, string clanName, string clanId, string region)
{
    public string ClanTag { get; } = clanTag;
    public string ClanName { get; } = clanName;
    public string ClanId { get; } = clanId;
    public string Region { get; } = region;

    public static string GetRegionCode(string region) => region switch
    {
        "eu" or "EU" => "EU",
        "asia" or "ASIA" => "ASIA",
        "com" => "NA",
        _ => "undefined"
    };
}