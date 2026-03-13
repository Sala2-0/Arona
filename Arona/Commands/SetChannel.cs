using NetCord;
using NetCord.Services.ApplicationCommands;
using Arona.Services.Message;
using Arona.Models.DB;
using Arona.Services;
using NetCord.Gateway;
using Guild = Arona.Models.DB.Guild;

namespace Arona.Commands;

public class SetChannel(GatewayClient gatewayClient, ErrorService errorService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("set_channel", "Set the channel for Arona to log.")]
    public async Task SetChannelAsync(
        [SlashCommandParameter(Name = "channel_id", Description = "Right click a channel and select \"Copy channel-ID\"")]
        string input
    )
    {
        var deferredMessage = await DeferredMessage.CreateAsync(Context.Interaction);

        Guild.Exists(Context.Interaction);

        string guildId = Context.Interaction.GuildId.ToString()!;

        await DatabaseService.WaitForWriteAsync(guildId);
        await DatabaseService.WaitForUpdateAsync();

        using var key = new DatabaseService.DatabaseWriteKey(guildId);

        ulong channelId = ulong.Parse(input);

        try
        {
            var channel = await gatewayClient.Rest.GetChannelAsync(channelId);

            if (channel.GetType() != typeof(TextGuildChannel))
            {
                await deferredMessage.EditAsync("❌ Specified channel is not a text channel.");
                return;
            }

            var guild = await gatewayClient.Rest.GetGuildAsync(Context.Guild!.Id);
            var self = await gatewayClient.Rest.GetGuildUserAsync(
                guildId: Context.Guild.Id,
                userId: (await gatewayClient.Rest.GetCurrentUserAsync()).Id
            );

            var guildChannel = channel as TextGuildChannel;
            var permissions = self.GetChannelPermissions(guild, guildChannel!);

            if ((permissions & Permissions.SendMessages) == 0)
            {
                await deferredMessage.EditAsync($"❌ I don't have permission to send messages in <#{channelId}>!");
                return;
            }

            var guildDb = Repository.Guilds.FindOne(g => g.Id == Context.Guild.Id.ToString());
            if (guildDb == null)
                Repository.Guilds.Insert(new Guild
                {
                    Id = Context.Guild.Id.ToString(),
                    ChannelId = channelId.ToString()
                });
            else
            {
                guildDb.ChannelId = channelId.ToString();
                Repository.Guilds.Update(guildDb);
            }

            await deferredMessage.EditAsync($"✅ Channel set to <#{channelId}>");
        }
        catch (Exception ex)
        {
            await errorService.PrintErrorAsync(ex, $"Error at {nameof(SetChannelAsync)}");
            await errorService.NotifyUserOfErrorAsync(Context.Interaction, ex, deferredMode: true, 
                "❌ Invalid channel ID format. Please provide a valid channel ID." +
                "\nCould also be that Arona doesn't have permissions to see specified channel");
        }
    }
}