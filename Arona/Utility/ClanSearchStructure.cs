namespace Arona.Utility;

public class ClanSearchStructure
{
    public string ClanTag;
    public string ClanName;
    public string ClanId;
    public string Region;
    
    public ClanSearchStructure(string clanTag, string clanName, string clanId, string region)
    {
        ClanTag = clanTag;
        ClanName = clanName;
        ClanId = clanId;
        Region = region;
    }

    public static string GetRegionCode(string region) => region switch
    {
        "eu" or "EU" => "EU",
        "asia" or "ASIA" => "ASIA",
        "com" => "NA",
        _ => "undefined"
    };
}