using NetCord;
using NetCord.Rest;

namespace Arona.Services.Message;

internal static class ChannelMessageService
{
    public static async Task SendAsync(ulong guildId, ulong channelId, EmbedProperties embed)
    {
        var self = await Program.Client!.Rest.GetGuildUserAsync(
            guildId: guildId,
            userId: (await Program.Client.Rest.GetCurrentUserAsync()).Id
        );

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
                await PrivateMessageService.SendNoPermissionMessageAsync(guildId, channel!.Name);
                return;
            }


            await Program.Client.Rest.SendMessageAsync(
                channelId: channelId,
                message: new MessageProperties { Embeds = [embed] }
            );
        }
        // Arona har inte tillgång/kan inte se kanalen
        catch (Exception ex)
        {
            await Program.Error(ex);
            await PrivateMessageService.SendNoAccessMessageAsync(guildId, channelId);
        }
    }
}