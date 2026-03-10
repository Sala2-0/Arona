using NetCord.Gateway;

namespace Arona.Models;

public interface IEmojiService
{
    public Task InitializeAsync();
}

internal class EmojiService(GatewayClient client) : IEmojiService
{
    public static string StageProgressVictory { get; private set; }
    public static string StageProgressDefeat { get; private set; }
    public static string StagePromoted { get; private set; }
    public static string StageDemoted { get; private set; }

    public async Task InitializeAsync()
    {
        var emojis = await client!.Rest.GetApplicationEmojisAsync(client.Id);

        StageProgressVictory = $"<:{nameof(StageProgressVictory)}:{emojis.First(x => x.Name == nameof(StageProgressVictory)).Id.ToString()}>";
        StageProgressDefeat = $"<:{nameof(StageProgressDefeat)}:{emojis.First(x => x.Name == nameof(StageProgressDefeat)).Id.ToString()}>";
        StagePromoted = $"<:{nameof(StagePromoted)}:{emojis.First(x => x.Name == nameof(StagePromoted)).Id.ToString()}>";
        StageDemoted = $"<:{nameof(StageDemoted)}:{emojis.First(x => x.Name == nameof(StageDemoted)).Id.ToString()}>";
    }
}