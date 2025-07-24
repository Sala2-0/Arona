namespace Arona.Commands;
using NetCord;
using MongoDB.Driver;
using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using Database;

public class SetChannel : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("set_channel", "Set the channel for Arona to log.")]
    public async Task SetChannelAsync(
        [SlashCommandParameter(Name = "channel_id", Description = "Right click a channel and select \"Copy channel-ID\"")] string channelId)
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());

        await Program.WaitForUpdateAsync();

        try
        {
            ulong channelIdParsed = ulong.Parse(channelId);

            var channel = await Program.Client!.Rest.GetChannelAsync(channelIdParsed);

            if (channel.GetType() != typeof(TextGuildChannel))
            {
                await Context.Interaction.ModifyResponseAsync(options =>
                    options.Content = "❌ Specified channel is not a text channel.");
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
                await Context.Interaction.ModifyResponseAsync(options =>
                    options.Content = $"❌ I don't have permission to send messages in <#{channelIdParsed}>!");
                return;
            }

            var res = await Program.Collection!.UpdateOneAsync(
                g => g.Id == Context.Guild.Id.ToString(),
                Builders<Guild>.Update.Set(g => g.ChannelId, channelIdParsed.ToString()),
                new UpdateOptions{ IsUpsert = true }
            );

            if (!res.IsAcknowledged)
            {
                await Context.Interaction.ModifyResponseAsync(options =>
                    options.Content = "❌ Error setting channel in database.");
                return;
            }

            await Context.Interaction.ModifyResponseAsync(options =>
                options.Content = $"✅ Channel set to <#{channelIdParsed}>");
        }
        catch (Exception ex)
        {
            await Context.Interaction.ModifyResponseAsync(options =>
                options.Content = "❌ Invalid channel ID format. Please provide a valid channel ID." +
                                  "\nCould also be that Arona doesn't have permissions to see specified channel");
        }
    }
}