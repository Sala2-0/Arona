using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona;

internal class Config
{
    public static string Token { get; private set; }
    public static string DevToken { get; private set; }
    public static string? ClientId { get; private set; }
    public static string? GuildId { get; private set; }
    public static string WgApi { get; private set; }
    public static string Database { get; private set; }

    public static void Initialize()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config.json");

        if (!File.Exists(path))
            throw new Exception("Configuration file not found.");

        string json = File.ReadAllText(path);
        
        try
        {
            var instance = JsonSerializer.Deserialize<ConfigInstance>(json);

            Token = instance!.Token;
            DevToken = instance.DevToken;
            ClientId = instance.ClientId;
            GuildId = instance.GuildId;
            WgApi = instance.WgApi;
            Database = instance.Database;
        }
        catch (JsonException ex)
        {
            throw new Exception("Failed to parse configuration file. Please ensure it is correctly formatted.", ex);
        }
    }

    private class ConfigInstance
    {
        [JsonPropertyName("token")] public required string Token { get; set; }
        [JsonPropertyName("dev_token")] public required string DevToken { get; set; }
        [JsonPropertyName("client_id")] public string? ClientId { get; set; }
        [JsonPropertyName("guild_id")] public string? GuildId { get; set; }
        [JsonPropertyName("wg_api")] public required string WgApi { get; set; }
        [JsonPropertyName("database")] public required string Database { get; set; }
    }
}