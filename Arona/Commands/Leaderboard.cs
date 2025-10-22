using System.Globalization;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Models;
using Arona.Models.DB;
using Arona.Models.Api.Clans;
using Arona.Utility;

namespace Arona.Commands;

public class Leaderboard : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("leaderboard", "Latest clan battles season leaderboard. Default: Hurricane I (Global) [Ratings]")]
    public async Task LeaderboardAsync(
        [SlashCommandParameter(Name = "league", Description = "League")]
        League league = League.Hurricane,

        [SlashCommandParameter(Name = "division", Description = "Division")]
        Division division = Division.I,

        [SlashCommandParameter(Name = "region", Description = "Region")]
        Realm realm = Realm.Global,

        [SlashCommandParameter(Name = "type", Description = "The clan parameter rankings will base on")]
        LeaderboardType leaderboardType = LeaderboardType.Ratings
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        Guild.Exists(Context.Interaction);

        if (league == League.Hurricane && division is Division.II or Division.III)
        {
            await deferredMessage.EditAsync($"❌ Hurricane {division} doesn't exist.");
            return;
        }

        try
        {
            var data = await LadderStructure.GetAsync((int)league, (int)division, realm.ToString().ToLower());

            if (data.Length == 0)
            {
                await deferredMessage.EditAsync("❌ No data found for the specified league and division.");
                return;
            }

            if (leaderboardType == LeaderboardType.Ratings)
            {
                var embed = new EmbedProperties()
                    .WithTitle(
                        $"Leaderboard - {league} {division} ({ClanUtils.ConvertRealm(realm.ToString().ToLower())}) [Ratings]")
                    .WithColor(new Color(Convert.ToInt32(ClanUtils.GetLeagueColor(league).TrimStart('#'), 16)));

                var fields = new List<EmbedFieldProperties>();

                foreach (var clan in data)
                {
                    string successFactor = SuccessFactor.Calculate(clan.PublicRating, clan.BattlesCount, ClanUtils.GetLeagueExponent(league))
                        .ToString("0.##", CultureInfo.InvariantCulture);

                    fields.Add(new EmbedFieldProperties()
                        .WithName(
                            $"**#{clan.Rank}** ({ClanUtils.ConvertRealm(clan.Realm)}) `[{clan.Tag}]` ({clan.DivisionRating}) `BTL: {clan.BattlesCount}` `S/F: {successFactor}`"));
                }

                embed.WithFields(fields);

                await Context.Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
            }

            else if (leaderboardType == LeaderboardType.SuccessFactor)
            {
                foreach (var clan in data)
                {
                    clan.SuccessFactor = Math.Round(
                        SuccessFactor.Calculate(clan.PublicRating, clan.BattlesCount, ClanUtils.GetLeagueExponent(league)),
                        2
                    );
                }

                var embed = new EmbedProperties()
                    .WithTitle(
                        $"Leaderboard - {league} {division} ({ClanUtils.ConvertRealm(realm.ToString().ToLower())}) [S/F]")
                    .WithColor(new Color(Convert.ToInt32(ClanUtils.GetLeagueColor(league), 16)));

                var fields = new List<EmbedFieldProperties>();

                var sortedStructure = data.OrderByDescending(s => s.SuccessFactor).ToList();

                for (int i = 0; i < sortedStructure.Count; i++)
                {
                    var clan = sortedStructure[i];
                    var successFactor = clan.SuccessFactor?.ToString(CultureInfo.InvariantCulture);

                    fields.Add(
                        new EmbedFieldProperties()
                            .WithName($"**#{i + 1}** ({ClanUtils.ConvertRealm(clan.Realm)}) `[{clan.Tag}]` ({clan.DivisionRating}) `S/F: {successFactor}` `BTL: {clan.BattlesCount}`")
                    );
                }

                embed.WithFields(fields);

                await deferredMessage.EditAsync(embed);
            }
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching leaderboard data from API.");
        }
    }
}

public enum Realm
{
    [SlashCommandChoice(Name = "Global")] Global,
    [SlashCommandChoice(Name = "EU")] Eu,
    [SlashCommandChoice(Name = "NA")] Us,
    [SlashCommandChoice(Name = "ASIA")] Sg
}

public enum LeaderboardType
{
    [SlashCommandChoice(Name = "Ratings")] Ratings,
    [SlashCommandChoice(Name = "S/F")] SuccessFactor
}