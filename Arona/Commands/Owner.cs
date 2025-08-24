using NetCord;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using Arona.Database;
using Arona.Utility;
using NetCord.Rest;

namespace Arona.Commands;

public class OwnerCommands : CommandModule<CommandContext>
{
    [Command("announce")]
    public async Task OwnerAsync(
        [CommandParameter(Remainder = true)] string content
    )
    {
        if (!Owner.Check(Context.User.Id)) return;

        var self = await Program.Client!.Rest.GetGuildUserAsync(
            guildId: Context.Guild!.Id,
            userId: (await Program.Client.Rest.GetCurrentUserAsync()).Id
        );

        var guilds = Collections.Guilds.FindAll().ToList();

        foreach (var guild in guilds)
        {
            try
            {
                var parsedChannelId = ulong.Parse(guild.ChannelId);

                var channel = await Program.Client.Rest.GetChannelAsync(parsedChannelId) as TextGuildChannel;

                var permissions = self.GetChannelPermissions(
                    guild: await Program.Client.Rest.GetGuildAsync(Context.Guild.Id),
                    channel: channel!
                );

                // Arona har inte tillstånd att skicka meddelanden
                if ((permissions & Permissions.SendMessages) == 0)
                {
                    await PrivateMessage.NoPermissionMessage(ulong.Parse(guild.Id), channel!.Name);
                    continue;
                }

                await Program.Client!.Rest.SendMessageAsync(
                    channelId: parsedChannelId,
                    message: content
                );
            }
            // Arona har inte tillgång/kan inte se kanalen
            catch (Exception ex)
            {
                await PrivateMessage.NoAccessMessage(ulong.Parse(guild.Id), guild.ChannelId);
            }
        }
    }

    [Command("guilds")]
    public async Task Guilds()
    {
        if (!Owner.Check(Context.User.Id)) return;

        string message = $"**{Program.Client!.Cache.Guilds.Count}** servrar har Arona tillagd\n\nServrar:";

        foreach (var guild in Program.Client.Cache.Guilds.Values)
            message += $"\n`{guild.Name}` (ID: {guild.Id})";

        await Context.Message.ReplyAsync(message);
    }
}

public class OwnerAppCommands : ApplicationCommandModule<ApplicationCommandContext>
{

}

internal class Owner
{
    internal static bool Check(ulong userId) => userId == 1203783301580460032;
}