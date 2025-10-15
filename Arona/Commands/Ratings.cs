using System.Text.Json;
using System.Text.Json.Serialization;
using Arona.ApiModels;
using Arona.Autocomplete;
using Arona.Models;
using Arona.Database;
using Arona.Utility;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Arona.Commands;

public class Ratings : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ratings", "Get detailed information about a clans current ratings on current CB season")]
    public async Task RatingsAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanIdAndRegion
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        Guild.Exists(Context.Interaction);

        var self = await Program.Client!.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        string[] split = clanIdAndRegion.Split(',');
        string region = split[1];
        int clanId = int.Parse(split[0]);

        using HttpClient client = new();
        Task<ClanBase.ClanView> generalTask = ClanBase.GetAsync(clanId, region);
        Task<LadderStructure[]> globalRankTask = LadderStructure.GetAsync(clanId: clanId, region: region);
        Task<LadderStructure[]> regionRankTask = LadderStructure.GetAsync(clanId: clanId, region: region, realm: LadderStructure.ConvertRegion(region));

        try
        {
            await Task.WhenAll(generalTask, globalRankTask, regionRankTask);

            var data = (
                Clan: await generalTask,
                Global: await globalRankTask,
                Region: await regionRankTask
            );

            int latestSeason = data.Clan.WowsLadder.SeasonNumber;
            var leadingTeamNumber = data.Clan.WowsLadder.LeadingTeamNumber;

            var clan = new Clan
            {
                Name = $"[{data.Clan.Clan.Tag}] {data.Clan.Clan.Name}",
                League = data.Clan.WowsLadder.League,
                Division = data.Clan.WowsLadder.Division,
                DivisionRating = data.Clan.WowsLadder.DivisionRating,
                Color = data.Clan.Clan.Color,
            };

            if (!data.Clan.WowsLadder.Ratings.Exists(team => team.SeasonNumber == latestSeason))
            {
                await deferredMessage.EditAsync(new EmbedProperties 
                {
                    Author = new EmbedAuthorProperties { Name = "Arona's intelligence report", IconUrl = botIconUrl },
                    Title = $"`[{data.Clan.Clan.Tag}] {data.Clan.Clan.Name}` ({ClanUtils.GetRegionCode(region)})",
                    Fields = [new EmbedFieldProperties{ Name = "Clan doesn't play clan battles." }]
                });

                return;
            }

            foreach (var rating in data.Clan.WowsLadder.Ratings.FindAll(team => team.SeasonNumber == latestSeason))
            {
                clan.Teams.Add(new Team
                {
                    TeamNumber = rating.TeamNumber,
                    Color = ClanUtils.GetLeagueColor(rating.League),
                    Battles = rating.BattlesCount,
                    SuccessFactor = SuccessFactor.Calculate(rating.PublicRating, rating.BattlesCount, ClanUtils.GetLeagueExponent(rating.League)),
                    WinRate = rating.BattlesCount > 0
                        ? Math.Round((double)rating.WinsCount / rating.BattlesCount * 100, 2)
                        : 0,
                    League = rating.League,
                    Division = rating.Division,
                    DivisionRating = rating.DivisionRating,
                    Stage = rating.Stage != null ? new Stage(rating.Stage.Type, rating.Stage.Progress) : null
                });
            }

            clan.Teams.Sort((a, b) => (int)a.TeamNumber - (int)b.TeamNumber);

            clan.Stage = data.Clan.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)?.Stage != null
                ? new Stage(
                    data.Clan.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Type,
                    data.Clan.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Progress
                )
                : null;

            // Hämta klanens ranking
            clan.GlobalRank = data.Global.Where(c => c.Id == clanId)
                .Select(c => c.Rank).FirstOrDefault();
            clan.RegionRank = data.Region.Where(c => c.Id == clanId)
                .Select(c => c.Rank).FirstOrDefault();

            var json = JsonSerializer.Serialize(clan);

            string body = $"{{\"data\":{json}}}";
            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync("http://localhost:3000/ratings", content);

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(imageBytes);
            
            await deferredMessage.Interaction.SendFollowupMessageAsync(
                new InteractionMessageProperties()
                    .WithAttachments([new AttachmentProperties($"{clanId}.png", stream)])
            );
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
        }
    }

    private class Clan
    {
        public string Name { get; init; }
        public string Color { get; init; }
        public ClanUtils.League League { get; init; }
        public ClanUtils.Division Division { get; init; }
        public int DivisionRating { get; init; }
        public int GlobalRank { get; set; }
        public int RegionRank { get; set; }
        public List<Team> Teams { get; } = [];
        public Stage? Stage { get; set; }
    }

    private class Team
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required ClanUtils.Team TeamNumber { get; init; }
        public string Color { get; init; }
        public required int Battles { get; init; }
        public required double WinRate { get; init; }
        public required double SuccessFactor { get; init; }
        public required ClanUtils.League League { get; init; }
        public required ClanUtils.Division Division { get; init; }
        public required int DivisionRating { get; init; }
        public required Stage? Stage { get; init; }
    }

    private class Stage(ClanUtils.StageType type, string[] progress)
    {
        public ClanUtils.StageType Type { get; } = type;
        public string[] Progress { get; } = progress;
    }
}