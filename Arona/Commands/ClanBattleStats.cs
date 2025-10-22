using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Models.Api.Official;
using Arona.Commands.Autocomplete;
using Arona.Models.DB;
using Arona.Models;
using Arona.Utility;

namespace Arona.Commands;

public class ClanBattleStats : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("cb_stats", "Clan battle stats of a player")]
    public async Task BaseExpAddAsync(
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
}