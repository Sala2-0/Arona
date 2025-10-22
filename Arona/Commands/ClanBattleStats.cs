using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Models.Api.Official;
using Arona.Commands.Autocomplete;
using Arona.Models.DB;
using Arona.Models;
using Arona.Utility;

namespace Arona.Commands;

[SlashCommand("cb_stats", "Clan battle stats of a player")]
public class ClanBattleStats : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("season", "One season")]
    public async Task SeasonAsync(
        [SlashCommandParameter(Name = "player", Description = "Player in question", AutocompleteProviderType = typeof(PlayerAutocomplete))]
        string accountData,

        [SlashCommandParameter(Name = "season_number", Description = "Season number, -1 for latest season")]
        int seasonNumber = -1
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        Guild.Exists(Context.Interaction);

        var self = await Program.Client!.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        var split = accountData.Split(',');
        long accountId = long.Parse(split[0]);
        string rawName = split[1];
        string region = split[2];

        string name = "";

        foreach (char letter in rawName)
            if (letter == '_')
                name += "\\_";
            else
                name += letter;

        try
        {
            var data = await ClanBattleSeasonStats.GetAsync(accountId.ToString(), region);
            var seasonData = await ClanBattleSeasons.GetAsync();

            if (seasonNumber == -1)
                seasonNumber = seasonData
                    .Select(s => int.Parse(s.Key))
                    .Where(id => id > seasonNumber && id < 100)
                    .DefaultIfEmpty(seasonNumber)
                    .Max();
            
            if (!seasonData.ContainsKey(seasonNumber.ToString()))
            {
                await deferredMessage.EditAsync("Invalid season number");
                return;
            }

            var season = seasonData[seasonNumber.ToString()];
            var playerSeasonData = data[accountId.ToString()].Seasons.FirstOrDefault(s => s.SeasonId == seasonNumber) ?? null;

            if (playerSeasonData == null)
            {
                await deferredMessage.EditAsync($"**{name} ({region.ToUpper()})** has not played any clan battles in **S{seasonNumber} {season.Name}**");
                return;
            }

            await deferredMessage.EditAsync(new EmbedProperties
            {
                Author = new EmbedAuthorProperties { Name = "Arona's intelligence report", IconUrl = botIconUrl },
                Title = $"{name} ({ClanUtils.GetHumanRegion(region)})",
                Color = new Color(Convert.ToInt32(PersonalRatingColors.GetColor((double)playerSeasonData.Wins / playerSeasonData.Battles * 100), 16)),
                Description =
                    $"**Season:** {season.Name} ({seasonNumber})\n" +
                    $"**Battles:** {playerSeasonData.Battles}\n\n" +

                    $"**W/B:** {Math.Round((double)playerSeasonData.Wins / playerSeasonData.Battles * 100, 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%"}\n" +
                    $"**D/B:** {Math.Round((double)playerSeasonData.DamageDealt / playerSeasonData.Battles).ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                    $"**K/B:** {Math.Round((double)playerSeasonData.Kills / playerSeasonData.Battles, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            });
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("API error >_<");
        }
    }

    [SubSlashCommand("activity", "List showing a player's most active seasons")]
    public async Task ActivityAsync(
        [SlashCommandParameter(Name = "player", Description = "Player in question", AutocompleteProviderType = typeof(PlayerAutocomplete))]
        string accountData
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        Guild.Exists(Context.Interaction);

        var self = await Program.Client!.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        var split = accountData.Split(',');
        var accountId = split[0];
        var rawName = split[1];
        var region = split[2];

        var name = "";

        foreach (var letter in rawName)
            if (letter == '_')
                name += "\\_";
            else
                name += letter;

        try
        {
            var data = await ClanBattleSeasonStats.GetAsync(accountId, region);
            var seasonData = await ClanBattleSeasons.GetAsync();

            var filtered = data[accountId].Seasons
                .Where(c => c.Battles > 0)
                .ToList();
            filtered.Sort((a, b) => b.Battles - a.Battles);

            var processed = filtered.Select(c => new
            {
                Id = c.SeasonId,
                Name = seasonData.First(s => s.Value.SeasonId == c.SeasonId).Value.Name,
                BattlesCount = c.Battles,
                WinsCount = c.Wins
            });

            var embed = new EmbedProperties
            {
                Author = new EmbedAuthorProperties { Name = "Arona's intelligence report", IconUrl = botIconUrl },
                Title = $"{name} ({ClanUtils.GetHumanRegion(region)})",
            };

            foreach (var season in processed)
                embed.AddFields(new EmbedFieldProperties
                {
                    Name = $"S{season.Id} {season.Name}",
                    Value = $"`{season.BattlesCount}` BTL -> `{Math.Round((double)season.WinsCount / season.BattlesCount * 100, 2)}%` W/B\n"
                });

            await deferredMessage.EditAsync(embed);
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("API error >_<");
        }
    }
}