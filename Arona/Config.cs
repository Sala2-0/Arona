using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona;

internal static class Config
{
    private static string _token;
    private static string _devToken;
    public static string? ClientId { get; private set; }
    public static string? GuildId { get; private set; }
    public static string WgApi { get; private set; }
    public static string Database { get; private set; }
    public static ulong BackdoorChannel { get; private set; } // Används för att skicka potentiella fel, missbruk osv
    public static string PrecenseStr { get; private set; }

    public static void Initialize()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config.json");

        if (!File.Exists(path))
            throw new Exception("Configuration file not found.");

        string json = File.ReadAllText(path);
        
        try
        {
            var instance = JsonSerializer.Deserialize<ConfigInstance>(json);

            _token = instance!.Token;
            _devToken = instance.DevToken;
            ClientId = instance.ClientId;
            GuildId = instance.GuildId;
            WgApi = instance.WgApi;
            Database = instance.Database;
            BackdoorChannel = ulong.Parse(instance.BackdoorChannel);
            PrecenseStr = instance.PresenceStr;
        }
        catch (JsonException ex)
        {
            throw new Exception("Failed to parse configuration file. Please ensure it is correctly formatted.", ex);
        }
    }
    
    public static string GetToken() => Debugger.IsAttached ? _devToken : _token;

    private class ConfigInstance
    {
        [JsonPropertyName("token")] public required string Token { get; set; }
        [JsonPropertyName("dev_token")] public required string DevToken { get; set; }
        [JsonPropertyName("client_id")] public string? ClientId { get; set; }
        [JsonPropertyName("guild_id")] public string? GuildId { get; set; }
        [JsonPropertyName("wg_api")] public required string WgApi { get; set; }
        [JsonPropertyName("database")] public required string Database { get; set; }
        [JsonPropertyName("backdoor_channel")] public required string BackdoorChannel { get; set; }
        [JsonPropertyName("presence_str")] public string PresenceStr { get; set; } = "/help";
    }
}