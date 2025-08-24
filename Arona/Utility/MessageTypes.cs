using NetCord;
using NetCord.Rest;

namespace Arona.Utility;

internal class DeferredMessage
{
    public required ApplicationCommandInteraction Interaction { get; init; }

    // Använd alltid SendAsync först innan EditAsync används
    public async Task SendAsync() =>
        await Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

    public async Task EditAsync(string message) =>
        await Interaction.ModifyResponseAsync(options => options.Content = message);

    public async Task EditAsync(EmbedProperties embed) =>
        await Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
}

internal class PrivateMessage
{
    public static async Task NoAccessMessage(ulong guildId, string channelId)
    {
        var parsedGuild = await Program.Client!.Rest.GetGuildAsync(guildId);

        var owner = await Program.Client.Rest.GetGuildUserAsync(guildId, parsedGuild.OwnerId);

        var ownerName = owner.Username;
        var ownerPrivateMsg = await owner.GetDMChannelAsync();

        await ownerPrivateMsg.SendMessageAsync(
            $"Heyyyy **{ownerName}**. Just a reminder that I don't have access to text channel `{channelId}` in your server `{parsedGuild.Name}`."
            + "\n\nPlease give me permission to see and send messages in the channel or give me another channel so I can continue announcing events :3"
            + "\n\nArona-chan"
        );
    }

    public static async Task NoPermissionMessage(ulong guildId, string channelName)
    {
        var parsedGuild = await Program.Client!.Rest.GetGuildAsync(guildId);

        var owner = await Program.Client.Rest.GetGuildUserAsync(guildId, parsedGuild.OwnerId);

        var ownerName = owner.Username;
        var ownerPrivateMsg = await owner.GetDMChannelAsync();

        await ownerPrivateMsg.SendMessageAsync(
            $"Heyyyy **{ownerName}**. Just a reminder that I don't have permission to send messages in channel `{channelName}` in your server `{parsedGuild.Name}`."
            + "\n\nPlease give me permission to send messages in the channel or give me another channel so I can continue announcing events :3"
            + "\n\nArona-chan"
        );
    }
}