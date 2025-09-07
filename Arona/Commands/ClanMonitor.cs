using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;
using Arona.Autocomplete;
using Arona.Database;
using Arona.Utility;

namespace Arona.Commands;

public class ClanMonitor : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("clan_monitor_add", "Add a clan to server database")]
    public async Task ClanMonitorAddAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanAutocomplete))] string clanIdAndRegion)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        string guildId = Context.Interaction.GuildId.ToString()!;
        string channelId = Context.Interaction.Channel.Id.ToString();

        await Program.WaitForWriteAsync(guildId);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guildId);

        using var client = new HttpClient();

        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        long clanIdParsed = long.Parse(clanId);

        var guild = Collections.Guilds.FindOne(g => g.Id == guildId);

        // Om guild inte finns, skapa en ny
        if (guild == null)
        {
            guild = new Guild
            {
                Id = guildId,
                ChannelId = channelId
            };
            Collections.Guilds.Insert(guild);
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

            var clan = JsonSerializer.Deserialize<Clanbase>(results[0], Converter.Options);
            int latestSeason = clan!.ClanView.WowsLadder.SeasonNumber;

            var globalRank = JsonSerializer.Deserialize<LadderStructure[]>(results[1]);
            var regionRank = JsonSerializer.Deserialize<LadderStructure[]>(results[2]);

            var dbClan = Collections.Clans.FindOne(c => c.Id == clanIdParsed);

            if (dbClan is not null)
            {
                dbClan.Guilds.Add(guild.Id);

                Collections.Clans.Update(dbClan);
            }
            else
            {
                Collections.Clans.Insert(
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
                        Ratings = clan.ClanView.WowsLadder.Ratings.Where(r => r.SeasonNumber == latestSeason)
                            .Select(r =>
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
                                        : null,
                                    BattlesCount = r.BattlesCount
                                }).ToList(),
                        GlobalRank = globalRank!.FirstOrDefault(r => r.Id == clanIdParsed)!.Rank,
                        RegionRank = regionRank!.FirstOrDefault(r => r.Id == clanIdParsed)!.Rank,
                        Guilds = [guild.Id],
                        SessionEndTime = UpdateClan.GetEndSession(clan.ClanView.WowsLadder.PrimeTime),
                        SeasonNumber = latestSeason
                    }
                );
            }

            guild.Clans.Add(clanIdParsed);

            Collections.Guilds.Update(guild);

            await deferredMessage.EditAsync($"✅ Added clan: `[{clan.ClanView.Clan.Tag}] {clan.ClanView.Clan.Name}`");
        }
        catch (Exception ex)
        {
            Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
        }
        finally
        {
            Program.ActiveWrites.Remove(guildId);
        }
    }
    
    [SlashCommand("clan_monitor_remove", "Remove a clan from server database")]
    public async Task ClanMonitorRemoveAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to remove", AutocompleteProviderType = typeof(ClanRemoveAutocomplete))] string clanId)
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

            Program.ActiveWrites.Remove(guildId);
            return;
        }

        long clanIdParsed = long.Parse(clanId);

        var guild = Collections.Guilds.FindOne(g => g.Id == guildId);
        var clan = Collections.Clans.FindOne(c => c.Id == clanIdParsed);

        if (clan == null)
        {
            await deferredMessage.EditAsync("❌ Clan does not exist in database.");

            Program.ActiveWrites.Remove(guildId);
            return;
        }

        var clanTag = clan.ClanTag;
        var clanName = clan.ClanName;

        guild.Clans.Remove(clanIdParsed);
        clan.Guilds.Remove(guildId);

        if (clan.Guilds.Count == 0)
            Collections.Clans.Delete(clanIdParsed);
        else
            Collections.Clans.Update(clan);

        Collections.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Removed clan: `[{clanTag}] {clanName}`");

        Program.ActiveWrites.Remove(guildId);
    }

    [SlashCommand("clan_monitor_list", "List all clans in server database")]
    public async Task ClanMonitorListAsync()
    {
        string? guildName = Context.Interaction.Guild?.Name;
        string? guildId = Context.Interaction.GuildId.ToString();

        Guild? guild = Collections.Guilds.FindOne(g => g.Id == guildId);

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
            var clan = Collections.Clans.FindOne(c => c.Id == clanId);

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