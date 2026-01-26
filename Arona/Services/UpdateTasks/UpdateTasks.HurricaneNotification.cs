using NetCord.Rest;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Services.Message;

namespace Arona.Services.UpdateTasks;

internal partial class UpdateTasks
{
    private static async Task NotifyHurricaneChangesAsync(
        List<Guild> guilds,
        List<LadderStructure> added,
        List<LadderStructure> removed
    )
    {
        foreach (var guild in guilds)
        {
            foreach (var clan in removed)
                await ChannelMessageService.SendAsync(
                    ulong.Parse(guild.Id),
                    ulong.Parse(guild.ChannelId),
                    new EmbedProperties
                    {
                        Title = "Clan dropped from Hurricane",
                        Description = $"`[{clan.Tag}] {clan.Name}` has dropped from Hurricane leaderboard!"
                    }
                );

            foreach (var clan in added)
                await ChannelMessageService.SendAsync(
                    ulong.Parse(guild.Id),
                    ulong.Parse(guild.ChannelId),
                    new EmbedProperties
                    {
                        Title = "New hurricane clan",
                        Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                    }
                );
        }
    }

    private static async Task NotifyNewHurricaneClansAsync(
        List<Guild> guilds,
        List<LadderStructure> added
    )
    {
        foreach (var guild in guilds)
        {
            foreach (var clan in added)
                await ChannelMessageService.SendAsync(
                    ulong.Parse(guild.Id),
                    ulong.Parse(guild.ChannelId),
                    new EmbedProperties
                    {
                        Title = "New hurricane clan",
                        Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                    }
                );
        }
    }
}