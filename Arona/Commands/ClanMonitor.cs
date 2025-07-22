namespace Arona.Commands;
using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Utility;
using MongoDB.Bson;
using MongoDB.Driver;
using Database;
using ApiModels;


public class ClanMonitor : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("clan_monitor_add", "Add a clan to server database")]
    public async Task ClanMonitorAdd(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanSearch))] string clanIdAndRegion)
    {
        // Skicka inledande svar
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());

        await Program.WaitForUpdateAsync();

        var client = new HttpClient();

        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        string? guildId = Context.Interaction.GuildId.ToString();
        string channelId = Context.Interaction.Channel.Id.ToString();

        var collection = Program.DatabaseClient!.GetDatabase("Arona")
            .GetCollection<Guild>("servers");

        var guild = await collection.Find(g => g.Id == guildId).FirstOrDefaultAsync();

        // Om guild inte finns, skapa en ny
        if (guild == null)
        {
            guild = new Guild
            {
                Id = guildId!,
                ChannelId = channelId
            };
            await collection.InsertOneAsync(guild);
        }

        if (guild.Clans!.ContainsKey(clanId))
        {
            await Context.Interaction.ModifyResponseAsync(options => options.Content = "❌ Clan already exists in database.");
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

            guild.Clans.Add(clanId, new Database.Clan
            {
                ClanId = long.Parse(clanId),
                Region = region,
                ClanTag = clan.ClanView.Clan.Tag,
                ClanName = clan.ClanView.Clan.Name,
                RecentBattles = new List<BsonDocument>(),
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
                GlobalRank = globalRank!.FirstOrDefault(r => r.Id == long.Parse(clanId))!.Rank,
                RegionRank = regionRank!.FirstOrDefault(r => r.Id == long.Parse(clanId))!.Rank
            });

            var res = await collection.ReplaceOneAsync(g => g.Id == guild.Id, guild);

            if (!res.IsAcknowledged)
            {
                await Context.Interaction.ModifyResponseAsync(options =>
                    options.Content = "❌ Error adding clan to database.");
                return;
            }

            await Context.Interaction.ModifyResponseAsync(options =>
                options.Content = $"✅ Added clan: `[{clan.ClanView.Clan.Tag}] {clan.ClanView.Clan.Name}`");
        }
        catch (Exception ex)
        {
            Program.ApiError(ex);
            await Context.Interaction.ModifyResponseAsync(options => options.Content = "❌ Error fetching clan data from API.");
        }
    }
    
    [SlashCommand("clan_monitor_remove", "Remove a clan from server database")]
    public async Task ClanMonitorRemove(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to remove", AutocompleteProviderType = typeof(ClanRemoveSearch))] string clanId)
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());

        await Program.WaitForUpdateAsync();

        string? guildId = Context.Interaction.GuildId.ToString();

        var collection = Program.DatabaseClient!.GetDatabase("Arona")
            .GetCollection<Guild>("servers");

        var guild = await collection.Find(g => g.Id == guildId).FirstOrDefaultAsync();

        if (guild == null)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ No database exists for this server. **Add a clan to initialize one.**"));
            return;
        }

        var clanTag = guild.Clans.FirstOrDefault(c => c.Key == clanId).Value.ClanTag;
        var clanName = guild.Clans.FirstOrDefault(c => c.Key == clanId).Value.ClanName;

        var update = Builders<Guild>.Update.Unset($"clans.{clanId}");

        var res = await collection.UpdateOneAsync(g => g.Id == guildId, update);

        if (!res.IsAcknowledged)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ Error removing clan from database."));
            return;
        }

        if (res.ModifiedCount == 0)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message($"❌ Clan does not exist in database."));
            return;
        }

        await Context.Interaction.ModifyResponseAsync(options =>
            options.Content = $"✅ Removed clan: `[{clanTag}] {clanName}`");
    }

    [SlashCommand("clan_monitor_list", "List all clans in server database")]
    public async Task ClanMonitorList()
    {
        string? guildName = Context.Interaction.Guild?.Name;
        string? guildId = Context.Interaction.GuildId.ToString();

        var collection = Program.DatabaseClient!.GetDatabase("Arona")
            .GetCollection<Guild>("servers");

        Guild? guild = await collection.Find(g => g.Id == guildId).FirstOrDefaultAsync();

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

        foreach (var clanEntry in guild.Clans)
            clans.Add($"`[{clanEntry.Value.ClanTag}] {clanEntry.Value.ClanName}` " +
                      $"({ClanSearchStructure.GetRegionCode(clanEntry.Value.Region)})");
        
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
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([ embed ])));
    }
}