using NetCord;
using NetCord.Rest;

namespace Arona.Models;

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

internal static class PrivateMessage
{
    public static async Task NoAccessMessageAsync(ulong guildId, ulong channelId)
    {
        var parsedGuild = await Program.Client!.Rest.GetGuildAsync(guildId);

        var owner = await Program.Client.Rest.GetGuildUserAsync(guildId, parsedGuild.OwnerId);

        var ownerName = owner.Username;
        var ownerPrivateMsg = await owner.GetDMChannelAsync();

        await ownerPrivateMsg.SendMessageAsync(
            $"Heyyyy **{ownerName}**. Just a reminder that I don't have access to text channel `{channelId}` in your server `{parsedGuild.Name}`."
            + "\n\nPlease give me permission to see and send messages in the channel or give me another channel so I can continue announcing events :3"
            + "\n\n- Arona"
        );
    }

    public static async Task NoPermissionMessageAsync(ulong guildId, string channelName)
    {
        var parsedGuild = await Program.Client!.Rest.GetGuildAsync(guildId);

        var owner = await Program.Client.Rest.GetGuildUserAsync(guildId, parsedGuild.OwnerId);

        var ownerName = owner.Username;
        var ownerPrivateMsg = await owner.GetDMChannelAsync();

        await ownerPrivateMsg.SendMessageAsync(
            $"Heyyyy **{ownerName}**. Just a reminder that I don't have permission to send messages in channel `{channelName}` in your server `{parsedGuild.Name}`."
            + "\n\nPlease give me permission to send messages in the channel or give me another channel so I can continue announcing events :3"
            + "\n\n- Arona"
        );
    }
}

internal static class Message
{
    public static async Task SendAsync(ulong guildId, ulong channelId, EmbedProperties embed)
    {
        var self = await Program.Client!.Rest.GetCurrentUserGuildUserAsync(guildId);

        try
        {
            var channel = await Program.Client.Rest.GetChannelAsync(channelId) as TextGuildChannel;

            var permissions = self.GetChannelPermissions(
                guild: await Program.Client.Rest.GetGuildAsync(guildId),
                channel: channel!
            );

            // Arona har inte tillstånd att skicka meddelanden
            if ((permissions & Permissions.SendMessages) == 0)
            {
                await PrivateMessage.NoPermissionMessageAsync(guildId, channel!.Name);
                return;
            }

            
            await Program.Client.Rest.SendMessageAsync(
                channelId: channelId,
                message: new MessageProperties{ Embeds = [embed] }
            );
        }
        // Arona har inte tillgång/kan inte se kanalen
        catch (Exception ex)
        {
            await Program.Error(ex);
            await PrivateMessage.NoAccessMessageAsync(guildId, channelId);
        }
    }
}