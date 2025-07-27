namespace Arona.Commands;
using NetCord;
using MongoDB.Driver;
using NetCord.Services.ApplicationCommands;
using Database;
using Utility;

public class SetChannel : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("set_channel", "Set the channel for Arona to log.")]
    public async Task SetChannelAsync(
        [SlashCommandParameter(Name = "channel_id", Description = "Right click a channel and select \"Copy channel-ID\"")] string channelId)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        await Program.WaitForUpdateAsync();

        try
        {
            ulong channelIdParsed = ulong.Parse(channelId);

            var channel = await Program.Client!.Rest.GetChannelAsync(channelIdParsed);

            if (channel.GetType() != typeof(TextGuildChannel))
            {
                await deferredMessage.EditAsync("❌ Specified channel is not a text channel.");
                return;
            }

            var guild = await Program.Client.Rest.GetGuildAsync(Context.Guild!.Id);
            var self = await Program.Client.Rest.GetGuildUserAsync(
                guildId: Context.Guild.Id,
                userId: (await Program.Client.Rest.GetCurrentUserAsync()).Id
            );

            var guildChannel = channel as TextGuildChannel;
            var permissions = self.GetChannelPermissions(guild, guildChannel!);

            if ((permissions & Permissions.SendMessages) == 0)
            {
                await deferredMessage.EditAsync($"❌ I don't have permission to send messages in <#{channelIdParsed}>!");
                return;
            }

            var res = await Program.GuildCollection!.UpdateOneAsync(
                g => g.Id == Context.Guild.Id.ToString(),
                Builders<Guild>.Update.Set(g => g.ChannelId, channelIdParsed.ToString()),
                new UpdateOptions{ IsUpsert = true }
            );

            if (!res.IsAcknowledged)
            {
                await deferredMessage.EditAsync("❌ Error setting channel in database.");
                return;
            }

            await deferredMessage.EditAsync($"✅ Channel set to <#{channelIdParsed}>");
        }
        catch (Exception ex)
        {
            await deferredMessage.EditAsync("❌ Invalid channel ID format. Please provide a valid channel ID." +
                                            "\nCould also be that Arona doesn't have permissions to see specified channel");
        }
    }
}