using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

using Timer = System.Timers.Timer;

namespace Arona.Services.Message;

public interface IChannelMessageService
{
    public Task SendAsync(ulong guildId, ulong channelId, MessageProperties properties);
    public Task SendAfterTimeoutAsync(ulong guildId, ulong channelId, MessageProperties properties, long battleTimeSeconds);
}

public class ChannelMessageService(GatewayClient client, IPrivateMessageService privateMessageService, IErrorService errorService) : IChannelMessageService
{
    public async Task SendAsync(ulong guildId, ulong channelId, MessageProperties properties)
    {
        var self = await client!.Rest.GetGuildUserAsync(
            guildId: guildId,
            userId: (await client.Rest.GetCurrentUserAsync()).Id
        );

        try
        {
            var channel = await client.Rest.GetChannelAsync(channelId) as TextGuildChannel;

            var permissions = self.GetChannelPermissions(
                guild: await client.Rest.GetGuildAsync(guildId),
                channel: channel!
            );

            // Arona har inte tillstånd att skicka meddelanden
            if ((permissions & Permissions.SendMessages) == 0)
            {
                await privateMessageService.SendNoPermissionMessageAsync(guildId, channel!.Name);
                return;
            }
            
            await client.Rest.SendMessageAsync(
                channelId: channelId,
                message: properties
            );
        }
        // Arona har inte tillgång/kan inte se kanalen
        catch (Exception ex)
        {
            await errorService.LogErrorAsync(ex);
            await privateMessageService.SendNoAccessMessageAsync(guildId, channelId);
        }
    }
    
    public async Task SendAfterTimeoutAsync(ulong guildId, ulong channelId, MessageProperties properties, long battleTimeSeconds)
    {
        battleTimeSeconds += 300;
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var timeDifference = battleTimeSeconds - currentTime;
        if (timeDifference <= 0)
        {
            await SendAsync(guildId, channelId, properties);

            return;
        }

        var timer = new Timer(timeDifference * 1000d);
        timer.AutoReset = false;
        timer.Elapsed += async (_, _) =>
        {
            timer.Dispose();
            await SendAsync(guildId, channelId, properties);
        };
        timer.Enabled = true;
        timer.Start();
    }
}