namespace Arona.Utility;

internal class ClanSearchStructure(string clanTag, string clanName, string clanId, string region)
{
    public string ClanTag = clanTag;
    public string ClanName = clanName;
    public string ClanId = clanId;
    public string Region = region;

    public static string GetRegionCode(string region) => region switch
    {
        "eu" or "EU" => "EU",
        "asia" or "ASIA" => "ASIA",
        "com" => "NA",
        _ => "undefined"
    };
}