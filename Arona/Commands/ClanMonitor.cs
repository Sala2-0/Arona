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
public class ClanMonitor(
    IDatabaseRepository repository,
    IDatabaseRepositoryService<Guild> repositoryService,
    IApiClient apiClient,
    IErrorService errorService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "Add a clan to server database")]
    public async Task ClanMonitorAddAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanIdAndRegion
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var guild = repositoryService.GetOrCreate(Context.Guild!.Id.ToString());
        guild.ChannelId = Context.Channel.Id.ToString();

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

        var ladderStructureQuery = new LadderStructureByClanQuery(apiClient.HttpClient);
        var clanViewQuery = new ClanViewQuery(apiClient.HttpClient);
        Task<ClanViewRoot> apiTask = clanViewQuery.GetAsync(new ClanViewRequest(region, clanId));
        Task<LadderStructure[]> apiGlobalRankTask = ladderStructureQuery.GetAsync(new LadderStructureByClanRequest(clanId, region));
        Task<LadderStructure[]> apiRegionRankTask = ladderStructureQuery.GetAsync(new LadderStructureByClanRequest(clanId, region, ClanUtils.ToRealm(region)));

        try
        {
            await Task.WhenAll(apiTask, apiGlobalRankTask, apiRegionRankTask);

            var data = new
            {
                Clan = apiTask.Result,
                Global = apiGlobalRankTask.Result.FirstOrDefault(c => c.Id == clanId),
                Region = apiRegionRankTask.Result.FirstOrDefault(c => c.Id == clanId)
            };

            var dbClan = repository.Clans.FindOne(c => c.Clan.Id == clanId);

            if (dbClan is not null)
            {
                dbClan.ExternalData.Guilds.Add(guild.Id);

                repository.Clans.Update(dbClan);
            }
            else
            {
                data.Clan.ClanView.WowsLadder.Ratings.RemoveAll(r => r.SeasonNumber != data.Clan.ClanView.WowsLadder.SeasonNumber);

                data.Clan.ClanView.ExternalData.Region = region;
                data.Clan.ClanView.ExternalData.GlobalRank = data.Global?.Rank;
                data.Clan.ClanView.ExternalData.RegionRank = data.Region?.Rank;

                if (data.Clan.ClanView.WowsLadder.PrimeTime != null)
                    data.Clan.ClanView.ExternalData.SessionEndTime = ClanUtils.GetEndSession(data.Clan.ClanView.WowsLadder.PrimeTime);

                data.Clan.ClanView.ExternalData.Guilds.Add(guild.Id);
                repository.Clans.Insert(data.Clan.ClanView.Clan.Id, data.Clan.ClanView);
            }

            guild.Clans.Add(clanId);

            repository.Guilds.Update(guild);

            await deferredMessage.EditAsync($"✅ Added clan: `[{data.Clan.ClanView.Clan.Tag}] {data.Clan.ClanView.Clan.Name}`");
        }
        catch (Exception ex)
        {
            await errorService.LogErrorAsync(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
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

        var guild = repositoryService.GetOrCreate(Context.Guild!.Id.ToString());
        guild.ChannelId = Context.Channel.Id.ToString();

        await DatabaseService.WaitForWriteAsync(guild.Id);
        await DatabaseService.WaitForUpdateAsync();

        using var key = new DatabaseService.DatabaseWriteKey(guild.Id);

        if (input == "undefined")
        {
            await deferredMessage.EditAsync("❌ No clan selected to remove.");
            return;
        }

        int clanId = int.Parse(input);
        var clan = repository.Clans.FindOne(c => c.Clan.Id == clanId);

        if (clan == null)
        {
            await deferredMessage.EditAsync("❌ Clan does not exist in database.");
            return;
        }

        guild.Clans.Remove(clanId);
        clan.ExternalData.Guilds.Remove(guild.Id);

        if (clan.ExternalData.Guilds.Count == 0)
            repository.Clans.Delete(clanId);
        else
            repository.Clans.Update(clan);

        repository.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Removed clan: `[{clan.Clan.Tag}] {clan.Clan.Name}`");
    }

    [SubSlashCommand("list", "List all clans in server database")]
    public async Task ClanMonitorListAsync()
    {
        string? guildName = Context.Interaction.Guild?.Name;

        var guild = repositoryService.GetOrCreate(Context.Guild!.Id.ToString());
        guild.ChannelId = Context.Channel.Id.ToString();

        if (guild.Clans.Count == 0)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message($"No clans currently monitored in `{guildName}`"));
            return;
        }
        
        List<string> clans = guild.Clans
            .Select(id =>
            {
                var clan = repository.Clans.FindById(id);
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