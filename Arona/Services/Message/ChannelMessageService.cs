using Arona.Models.DB;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Arona.Services.Message;

public class ChannelMessageService(GatewayClient gatewayClient, ErrorService errorService)
{
    public async Task SendAsync(ulong guildId, ulong channelId, MessageProperties properties)
    {
        var self = await gatewayClient.Rest.GetGuildUserAsync(
            guildId: guildId,
            userId: (await gatewayClient.Rest.GetCurrentUserAsync()).Id
        );

        try
        {
            var channel = await gatewayClient.Rest.GetChannelAsync(channelId) as TextGuildChannel;

            var permissions = self.GetChannelPermissions(
                guild: await gatewayClient.Rest.GetGuildAsync(guildId),
                channel: channel!
            );

            // Arona har inte tillstånd att skicka meddelanden
            if ((permissions & Permissions.SendMessages) == 0)
            {
                return;
            }
            
            await gatewayClient.Rest.SendMessageAsync(
                channelId: channelId,
                message: properties
            );
        }
        // Arona har inte tillgång/kan inte se kanalen
        catch (Exception ex)
        {
            await errorService.PrintErrorAsync(ex);

            var guildData = Repository.Guilds.FindOne(guildId.ToString());
            guildData.ChannelId = null;
            Repository.Guilds.Update(guildId, guildData);
        }
    }

    public async Task SendAfterTimeoutAsync(ulong guildId, ulong channelId, MessageProperties properties, long timeout)
    {
        timeout += 300;
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var timeDifference = timeout - currentTime;
        if (timeDifference > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(timeDifference));
        }
        
        await SendAsync(guildId, channelId, properties);
    }
}