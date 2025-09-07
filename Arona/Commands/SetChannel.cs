﻿using NetCord;
using NetCord.Services.ApplicationCommands;
using Arona.Database;
using Arona.Utility;

namespace Arona.Commands;

public class SetChannel : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("set_channel", "Set the channel for Arona to log.")]
    public async Task SetChannelAsync(
        [SlashCommandParameter(Name = "channel_id", Description = "Right click a channel and select \"Copy channel-ID\"")] string channelId)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        string guildId = Context.Interaction.GuildId.ToString()!;

        await Program.WaitForWriteAsync(guildId);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guildId);

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

            var guildDb = Collections.Guilds.FindOne(g => g.Id == Context.Guild.Id.ToString());
            if (guildDb == null)
                Collections.Guilds.Insert(new Guild
                {
                    Id = Context.Guild.Id.ToString(),
                    ChannelId = channelIdParsed.ToString()
                });
            else
            {
                guildDb.ChannelId = channelIdParsed.ToString();
                Collections.Guilds.Update(guildDb);
            }

            await deferredMessage.EditAsync($"✅ Channel set to <#{channelIdParsed}>");
        }
        catch (Exception ex)
        {
            await deferredMessage.EditAsync("❌ Invalid channel ID format. Please provide a valid channel ID." +
                                            "\nCould also be that Arona doesn't have permissions to see specified channel");
        }
        finally
        {
            Program.ActiveWrites.Remove(guildId);
        }
    }
}