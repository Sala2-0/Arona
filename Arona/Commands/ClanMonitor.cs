namespace Arona.Commands;
using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Utility;
using MongoDB.Driver;
using Database;
using ApiModels;

public class ClanMonitor : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("clan_monitor_add", "Add a clan to server database")]
    public async Task ClanMonitorAddAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanSearch))] string clanIdAndRegion)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        string guildId = Context.Interaction.GuildId.ToString()!;
        string channelId = Context.Interaction.Channel.Id.ToString();

        await Program.WaitForWriteAsync(guildId);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guildId);

        var client = new HttpClient();

        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        long clanIdParsed = long.Parse(clanId);

        var guild = await Program.GuildCollection.Find(g => g.Id == guildId).FirstOrDefaultAsync();

        // Om guild inte finns, skapa en ny
        if (guild == null)
        {
            guild = new Guild
            {
                Id = guildId,
                ChannelId = channelId
            };
            await Program.GuildCollection!.InsertOneAsync(guild);
        }

        if (guild.Clans.Contains(clanIdParsed))
        {
            await deferredMessage.EditAsync("❌ Clan already exists in database.");
            return;
        }

        Task<string> apiTask = client.GetStringAsync(Clanbase.GetApiUrl(clanId, region));
        Task<string> apiGlobalRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId, region));
        Task<string> apiRegionRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId, region, LadderStructure.ConvertRegion(region)));

        try
        {
            var results = await Task.WhenAll(apiTask, apiGlobalRankTask, apiRegionRankTask);

            var clan = JsonSerializer.Deserialize<Clanbase>(results[0]);
            int latestSeason = clan!.ClanView.WowsLadder.SeasonNumber;

            var globalRank = JsonSerializer.Deserialize<LadderStructure[]>(results[1]);
            var regionRank = JsonSerializer.Deserialize<LadderStructure[]>(results[2]);

            var dbClan = await Program.ClanCollection.Find(c => c.Id == clanIdParsed).FirstOrDefaultAsync();

            if (dbClan is not null)
            {
                dbClan.Guilds.Add(guild.Id);

                var clanRes = await Program.ClanCollection!.ReplaceOneAsync(
                    c => c.Id == clanIdParsed,
                    dbClan
                );

                if (!clanRes.IsAcknowledged)
                {
                    await deferredMessage.EditAsync("❌ Error saving clan to database.");
                    return;
                }
            }
            else
            {
                await Program.ClanCollection!.InsertOneAsync(
                    new Database.Clan 
                    {
                        Id = clanIdParsed,
                        Region = region,
                        ClanTag = clan.ClanView.Clan.Tag,
                        ClanName = clan.ClanView.Clan.Name,
                        RecentBattles = [],
                        PrimeTime = new Database.PrimeTime
                        {
                            Planned = clan.ClanView.WowsLadder.PlannedPrimeTime,
                            Active = clan.ClanView.WowsLadder.PrimeTime
                        },
                        Ratings = clan.ClanView.WowsLadder.Ratings.Where(r => r.SeasonNumber == latestSeason).Select(r =>
                            new Database.Rating
                            {
                                TeamNumber = r.TeamNumber,
                                League = r.League,
                                Division = r.Division,
                                DivisionRating = r.DivisionRating,
                                PublicRating = r.PublicRating,
                                Stage = r.Stage != null
                                    ? new Database.Stage
                                    {
                                        Type = r.Stage.Type,
                                        TargetLeague = r.Stage.TargetLeague,
                                        TargetDivision = r.Stage.TargetDivision,
                                        Progress = r.Stage.Progress.ToList(),
                                        Battles = r.Stage.Battles,
                                        VictoriesRequired = r.Stage.VictoriesRequired
                                    }
                                    : null
                            }).ToList(),
                        GlobalRank = globalRank!.FirstOrDefault(r => r.Id == clanIdParsed)!.Rank,
                        RegionRank = regionRank!.FirstOrDefault(r => r.Id == clanIdParsed)!.Rank,
                        Guilds = [guild.Id],
                        SessionEndTime = UpdateClan.GetEndSession(clan.ClanView.WowsLadder.PrimeTime)
                    }
                );
            }

            guild.Clans.Add(clanIdParsed);

            var guildRes = await Program.GuildCollection!.ReplaceOneAsync(
                g => g.Id == guild.Id,
                guild
            );

            if (!guildRes.IsAcknowledged)
            {
                await deferredMessage.EditAsync("❌ Error saving clan to database.");
                return;
            }

            await deferredMessage.EditAsync($"✅ Added clan: `[{clan.ClanView.Clan.Tag}] {clan.ClanView.Clan.Name}`");

            Program.ActiveWrites.Remove(guildId);
        }
        catch (Exception ex)
        {
            Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
        }
    }
    
    [SlashCommand("clan_monitor_remove", "Remove a clan from server database")]
    public async Task ClanMonitorRemoveAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to remove", AutocompleteProviderType = typeof(ClanRemoveSearch))] string clanId)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        string guildId = Context.Interaction.GuildId.ToString()!;

        await Program.WaitForWriteAsync(guildId);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guildId);

        if (clanId == "undefined")
        {
            await deferredMessage.EditAsync("❌ No clan selected to remove.");
            return;
        }

        long clanIdParsed = long.Parse(clanId);

        var guild = await Program.GuildCollection!.Find(g => g.Id == guildId).FirstOrDefaultAsync();
        var clan = await Program.ClanCollection!.Find(c => c.Id == clanIdParsed).FirstOrDefaultAsync();

        if (clan == null)
        {
            await deferredMessage.EditAsync("❌ Clan does not exist in database.");
            return;
        }

        var clanTag = clan.ClanTag;
        var clanName = clan.ClanName;

        guild.Clans.Remove(clanIdParsed);
        clan.Guilds.Remove(guildId);

        if (clan.Guilds.Count == 0)
            await Program.ClanCollection!.DeleteOneAsync(c => c.Id == clanIdParsed);
        else
            await Program.ClanCollection!.ReplaceOneAsync(c => c.Id == clanIdParsed, clan);

        var res = await Program.GuildCollection!.ReplaceOneAsync(g => g.Id == guildId, guild);

        if (!res.IsAcknowledged)
        {
            await deferredMessage.EditAsync("❌ Error removing clan from database.");
            return;
        }

        await deferredMessage.EditAsync($"✅ Removed clan: `[{clanTag}] {clanName}`");

        Program.ActiveWrites.Remove(guildId);
    }

    [SlashCommand("clan_monitor_list", "List all clans in server database")]
    public async Task ClanMonitorListAsync()
    {
        string? guildName = Context.Interaction.Guild?.Name;
        string? guildId = Context.Interaction.GuildId.ToString();

        Guild? guild = await Program.GuildCollection!.Find(g => g.Id == guildId).FirstOrDefaultAsync();

        if (guild == null)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ No database exists for this server. **Add a clan to initialize one.**"));
            return;
        }

        if (guild.Clans.Count == 0)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message($"No clans currently monitored in `{guildName}`"));
            return;
        }
        
        List<string> clans = new List<string>();

        foreach (var clanId in guild.Clans)
        {
            var clan = await Program.ClanCollection.Find(c => c.Id == clanId).FirstOrDefaultAsync();

            clans.Add($"`[{clan.ClanTag}] {clan.ClanName}` " +
                      $"({ClanSearchStructure.GetRegionCode(clan.Region)})");
        }

        var field = new List<EmbedFieldProperties>();

        foreach (string clanName in clans)
        {
            field.Add(new EmbedFieldProperties()
                .WithName(clanName)
                .WithInline(false));
        }
        
        var embed = new EmbedProperties()
            .WithTitle($"Clans currently monitored in `{guildName}`")
            .WithFields(field);
        
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties()
                    .WithEmbeds([ embed ]))
        );
    }
}