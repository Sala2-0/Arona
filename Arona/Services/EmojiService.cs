using NetCord.Gateway;

namespace Arona.Services;

public class EmojiService(GatewayClient gatewayClient)
{
    public static string StageProgressVictory { get; private set; }
    public static string StageProgressDefeat { get; private set; }
    public static string StagePromoted { get; private set; }
    public static string StageDemoted { get; private set; }

    public async Task InitializeAsync()
    {
        var emojis = await gatewayClient.Rest.GetApplicationEmojisAsync(gatewayClient.Id);

        StageProgressVictory = $"<:{nameof(StageProgressVictory)}:{emojis.First(x => x.Name == nameof(StageProgressVictory)).Id.ToString()}>";
        StageProgressDefeat = $"<:{nameof(StageProgressDefeat)}:{emojis.First(x => x.Name == nameof(StageProgressDefeat)).Id.ToString()}>";
        StagePromoted = $"<:{nameof(StagePromoted)}:{emojis.First(x => x.Name == nameof(StagePromoted)).Id.ToString()}>";
        StageDemoted = $"<:{nameof(StageDemoted)}:{emojis.First(x => x.Name == nameof(StageDemoted)).Id.ToString()}>";
    }
}