using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Models;
using Arona.Models.DB;
using Arona.Models.Api.Clans;
using Arona.Utility;

namespace Arona.Commands;

[SlashCommand("clan_monitor", "Monitors a clan's clan battle activity")]
public class ClanMonitor : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "Add a clan to server database")]
    public async Task ClanMonitorAddAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanIdAndRegion
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var guild = Guild.Find(Context.Interaction);

        await Program.WaitForWriteAsync(guild.Id);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guild.Id);

        string[] split = clanIdAndRegion.Split(',');
        string region = split[1];
        int clanId = int.Parse(split[0]);

        if (guild.Clans.Contains(clanId))
        {
            await deferredMessage.EditAsync("❌ Clan already exists in database.");
            
            Program.ActiveWrites.Remove(guild.Id);
            return;
        }

        Task<ClanView> apiTask = ClanView.GetAsync(clanId, region);
        Task<LadderStructure[]> apiGlobalRankTask = LadderStructure.GetAsync(clanId, region);
        Task<LadderStructure[]> apiRegionRankTask = LadderStructure.GetAsync(clanId, region, ClanUtils.ConvertRegion(region));

        try
        {
            await Task.WhenAll(apiTask, apiGlobalRankTask, apiRegionRankTask);

            var data = new
            {
                Clan = apiTask.Result,
                Global = apiGlobalRankTask.Result.First(c => c.Id == clanId),
                Region = apiRegionRankTask.Result.First(c => c.Id == clanId)
            };

            var dbClan = Collections.Clans.FindOne(c => c.Clan.Id == clanId);

            if (dbClan is not null)
            {
                dbClan.ExternalData.Guilds.Add(guild.Id);

                Collections.Clans.Update(dbClan);
            }
            else
            {
                data.Clan.WowsLadder.Ratings.RemoveAll(r => r.SeasonNumber != data.Clan.WowsLadder.SeasonNumber);

                data.Clan.ExternalData.Region = region;
                data.Clan.ExternalData.GlobalRank = data.Global.Rank;
                data.Clan.ExternalData.RegionRank = data.Region.Rank;

                if (data.Clan.WowsLadder.PrimeTime != null)
                    data.Clan.ExternalData.SessionEndTime = ClanUtils.GetEndSession(data.Clan.WowsLadder.PrimeTime);

                data.Clan.ExternalData.Guilds.Add(guild.Id);
                Collections.Clans.Insert(data.Clan.Clan.Id, data.Clan);
            }

            guild.Clans.Add(clanId);

            Collections.Guilds.Update(guild);

            await deferredMessage.EditAsync($"✅ Added clan: `[{data.Clan.Clan.Tag}] {data.Clan.Clan.Name}`");
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
        }
        finally
        {
            Program.ActiveWrites.Remove(guild.Id);
        }
    }
    
    [SubSlashCommand("remove", "Remove a clan from server database")]
    public async Task ClanMonitorRemoveAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to remove", AutocompleteProviderType = typeof(ClanListAutocomplete))]
        string input
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var guild = Guild.Find(Context.Interaction);

        await Program.WaitForWriteAsync(guild.Id);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guild.Id);

        if (input == "undefined")
        {
            await deferredMessage.EditAsync("❌ No clan selected to remove.");

            Program.ActiveWrites.Remove(guild.Id);
            return;
        }

        int clanId = int.Parse(input);
        var clan = Collections.Clans.FindOne(c => c.Clan.Id == clanId);

        if (clan == null)
        {
            await deferredMessage.EditAsync("❌ Clan does not exist in database.");

            Program.ActiveWrites.Remove(guild.Id);
            return;
        }

        guild.Clans.Remove(clanId);
        clan.ExternalData.Guilds.Remove(guild.Id);

        if (clan.ExternalData.Guilds.Count == 0)
            Collections.Clans.Delete(clanId);
        else
            Collections.Clans.Update(clan);

        Collections.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Removed clan: `[{clan.Clan.Tag}] {clan.Clan.Name}`");

        Program.ActiveWrites.Remove(guild.Id);
    }

    [SubSlashCommand("list", "List all clans in server database")]
    public async Task ClanMonitorListAsync()
    {
        string? guildName = Context.Interaction.Guild?.Name;

        var guild = Guild.Find(Context.Interaction);

        if (guild.Clans.Count == 0)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message($"No clans currently monitored in `{guildName}`"));
            return;
        }
        
        List<string> clans = guild.Clans
            .Select(id =>
            {
                var clan = Collections.Clans.FindById(id);
                return $"`[{clan.Clan.Tag}] {clan.Clan.Name}` ({ClanUtils.GetHumanRegion(clan.ExternalData.Region)})";
            })
            .ToList();

        List<EmbedFieldProperties> field = clans
            .Select(n => new EmbedFieldProperties{ Name = n })
            .ToList();
        
        var embed = new EmbedProperties()
            .WithTitle($"Clans currently monitored in `{guildName}`")
            .WithFields(field);
        
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties()
                    .WithEmbeds([ embed ]))
        );
    }

    [SubSlashCommand("set_cookie", "Add cookie for one a clan for detailed data")]
    public async Task SetCookieAsync(
        [SlashCommandParameter(Name = "clan", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanMetadata,

        [SlashCommandParameter(Name = "player", Description = "The player the cookie belongs to", AutocompleteProviderType = typeof(PlayerAutocomplete))]
        string accountMetadata
    )
    {
        var guild = Guild.Find(Context.Interaction);

        await Program.WaitForWriteAsync(guild.Id);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guild.Id);

        var s = (
            account: accountMetadata.Split(','),
            clan: clanMetadata.Split(',')
        );
        var accountId = long.Parse(s.account[0]);
        var username = s.account[1];
        var region = s.clan[1];
        var clanId = int.Parse(s.clan[0]);

        var modal = new ModalProperties($"cookie form:{accountId}:{region}:{clanId}", "Cookie")
        {
            Components = [
                new TextInputProperties("cookie", TextInputStyle.Short, $"Enter your cookie for {username}")
            ]
        };

        await Context.Interaction.SendResponseAsync(InteractionCallback.Modal(modal));
    }
}