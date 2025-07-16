namespace Arona.Config;
using System.Text.Json.Serialization;

internal class BotConfig
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    [JsonPropertyName("dev_token")]
    public string? DevToken { get; set; }
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    [JsonPropertyName("guild_id")]
    public string? GuildId { get; set; }
    [JsonPropertyName("wg_api")]
    public string? WgApi { get; set; }
    [JsonPropertyName("database_url")]
    public string? Database { get; set; }

    public static string GetConfigFilePath() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Config", "config.json"));
}