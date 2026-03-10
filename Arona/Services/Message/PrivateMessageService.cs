using NetCord.Gateway;
using NetCord.Rest;

namespace Arona.Services.Message;

public interface IPrivateMessageService
{
    public Task SendNoAccessMessageAsync(ulong guildId, ulong channelId);
    public Task SendNoPermissionMessageAsync(ulong guildId, string channelName);
}

public class PrivateMessageService(GatewayClient client) : IPrivateMessageService
{
    public async Task SendNoAccessMessageAsync(ulong guildId, ulong channelId)
    {
        var parsedGuild = await client!.Rest.GetGuildAsync(guildId);

        var owner = await client.Rest.GetGuildUserAsync(guildId, parsedGuild.OwnerId);

        var ownerName = owner.Username;
        var ownerPrivateMsg = await owner.GetDMChannelAsync();

        await ownerPrivateMsg.SendMessageAsync(
            $"Heyyyy **{ownerName}**. Just a reminder that I don't have access to text channel `{channelId}` in your server `{parsedGuild.Name}`."
            + "\n\nPlease give me permission to see and send messages in the channel or give me another channel so I can continue announcing events :3"
            + "\n\n- Arona"
        );
    }

    public async Task SendNoPermissionMessageAsync(ulong guildId, string channelName)
    {
        var parsedGuild = await client!.Rest.GetGuildAsync(guildId);

        var owner = await client.Rest.GetGuildUserAsync(guildId, parsedGuild.OwnerId);

        var ownerName = owner.Username;
        var ownerPrivateMsg = await owner.GetDMChannelAsync();

        await ownerPrivateMsg.SendMessageAsync(
            $"Heyyyy **{ownerName}**. Just a reminder that I don't have permission to send messages in channel `{channelName}` in your server `{parsedGuild.Name}`."
            + "\n\nPlease give me permission to send messages in the channel or give me another channel so I can continue announcing events :3"
            + "\n\n- Arona"
        );
    }
}