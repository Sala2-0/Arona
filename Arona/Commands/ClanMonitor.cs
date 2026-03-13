using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Services.Message;
using Arona.Models.DB;
using Arona.Models.Api.Clans;
using Arona.Services;
using Arona.Utility;

namespace Arona.Commands;

[SlashCommand("clan_monitor", "Monitors a clan's clan battle activity")]
public class ClanMonitor(ErrorService errorService, IApiService apiService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "Add a clan to server database")]
    public async Task ClanMonitorAddAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanIdAndRegion
    )
    {
        var deferredMessage = await DeferredMessage.CreateAsync(Context.Interaction);

        var guild = Guild.Find(Context.Interaction);

        if (guild.Clans.Count >= 5)
        {
            await deferredMessage.EditAsync("❌ Maximum of 5 clans can be monitored per server.");
            return;
        }

        await DatabaseService.WaitForWriteAsync(guild.Id);
        await DatabaseService.WaitForUpdateAsync();

        using var key = new DatabaseService.DatabaseWriteKey(guild.Id);

        string[] split = clanIdAndRegion.Split(',');
        string region = split[1];
        int clanId = int.Parse(split[0]);

        if (guild.Clans.Contains(clanId))
        {
            await deferredMessage.EditAsync("❌ Clan already exists in database.");
            return;
        }
        
        var clanData = await new ClanViewQuery(apiService.HttpClient)
            .GetAsync(new ClanViewRequest(region, clanId))
            .IgnoreRedundantFields();
        var clanRank = await new LadderStructureByClanQuery(apiService.HttpClient).GetRegionAndGlobalRankAsync(clanId, region);

        try
        {
            var dbClan = Repository.Clans.FindOne(c => c.Clan.Id == clanId);

            if (dbClan is not null)
            {
                dbClan.ExternalData.Guilds.Add(guild.Id);

                Repository.Clans.Update(dbClan);
            }
            else
            {
                clanData.FilterRatings(clanData.WowsLadder.SeasonNumber);

                clanData.ExternalData = new External
                {
                    Region = region,
                    RankData = clanRank
                };

                if (clanData.WowsLadder.PrimeTime != null)
                    clanData.ExternalData.SessionEndTime = ClanUtils.GetEndSession(clanData.WowsLadder.PrimeTime);

                clanData.ExternalData.Guilds.Add(guild.Id);
                Repository.Clans.Insert(clanData.Clan.Id, clanData);
            }

            guild.Clans.Add(clanId);

            Repository.Guilds.Update(guild);

            await deferredMessage.EditAsync($"✅ Added clan: `[{clanData.Clan.Tag}] {clanData.Clan.Name}`");
        }
        catch (Exception ex)
        {
            await errorService.PrintErrorAsync(ex, $"Error in {nameof(ClanMonitorAddAsync)}");
            await errorService.NotifyUserOfErrorAsync(Context.Interaction, ex, deferredMode: true);
        }
    }
    
    [SubSlashCommand("remove", "Remove a clan from server database")]
    public async Task ClanMonitorRemoveAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to remove", AutocompleteProviderType = typeof(ClanListAutocomplete))]
        string input
    )
    {
        var deferredMessage = await DeferredMessage.CreateAsync(Context.Interaction);

        var guild = Guild.Find(Context.Interaction);

        await DatabaseService.WaitForWriteAsync(guild.Id);
        await DatabaseService.WaitForUpdateAsync();

        using var key = new DatabaseService.DatabaseWriteKey(guild.Id);

        if (input == "undefined")
        {
            await deferredMessage.EditAsync("❌ No clan selected to remove.");
            return;
        }

        int clanId = int.Parse(input);
        var clan = Repository.Clans.FindOne(c => c.Clan.Id == clanId);

        if (clan == null)
        {
            await deferredMessage.EditAsync("❌ Clan does not exist in database.");
            return;
        }

        guild.Clans.Remove(clanId);
        clan.ExternalData.Guilds.Remove(guild.Id);

        if (clan.ExternalData.Guilds.Count == 0)
            Repository.Clans.Delete(clanId);
        else
            Repository.Clans.Update(clan);

        Repository.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Removed clan: `[{clan.Clan.Tag}] {clan.Clan.Name}`");
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
                var clan = Repository.Clans.FindById(id);
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