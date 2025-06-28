namespace Arona.Config;
using System.Text.Json.Serialization;

public class BotConfig
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    [JsonPropertyName("guild_id")]
    public string? GuildId { get; set; }
    [JsonPropertyName("wg_api")]
    public string? WgApi { get; set; }
    [JsonPropertyName("server_api")]
    public string? ServerApi { get; set; }
    [JsonPropertyName("database")]
    public string? Database { get; set; }

    public static string GetConfigFilePath() =>
        File.ReadAllText(Path.Combine("..", "..", "..", "Config", "config.json"));
}