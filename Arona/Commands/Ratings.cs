using System.Text.Json;
using System.Text.Json.Serialization;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Models;
using Arona.Models.DB;
using Arona.Models.Api.Clans;
using Arona.Utility;
using Arona.Services.Message;

using TeamNumber = Arona.Models.Team;

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

        Task<ClanViewRoot> generalTask = ClanViewQuery.GetSingleAsync(new ClanViewRequest(region, clanId));

        var ladderStructureQuery = new LadderStructureByClanQuery(ApiClient.Instance);
        Task<LadderStructure[]> 
            globalRankTask = ladderStructureQuery.GetAsync(new LadderStructureByClanRequest(clanId, region)),
            regionRankTask = ladderStructureQuery.GetAsync(new LadderStructureByClanRequest(clanId, region, ClanUtils.ToRealm(region)));

        try
        {
            await Task.WhenAll(generalTask, globalRankTask, regionRankTask);

            var data = (
                Clan: await generalTask,
                Global: await globalRankTask,
                Region: await regionRankTask
            );

            int latestSeason = data.Clan.ClanView.WowsLadder.SeasonNumber;
            var leadingTeamNumber = data.Clan.ClanView.WowsLadder.LeadingTeamNumber;

            var clan = new ClanDto
            {
                Name = $"[{data.Clan.ClanView.Clan.Tag}] {data.Clan.ClanView.Clan.Name}",
                League = data.Clan.ClanView.WowsLadder.League,
                Division = data.Clan.ClanView.WowsLadder.Division,
                DivisionRating = data.Clan.ClanView.WowsLadder.DivisionRating,
                Color = data.Clan.ClanView.Clan.Color,
            };

            if (!data.Clan.ClanView.WowsLadder.Ratings.Exists(team => team.SeasonNumber == latestSeason))
            {
                await deferredMessage.EditAsync(new EmbedProperties 
                {
                    Author = new EmbedAuthorProperties { Name = "Arona's intelligence report", IconUrl = botIconUrl },
                    Title = $"`[{data.Clan.ClanView.Clan.Tag}] {data.Clan.ClanView.Clan.Name}` ({ClanUtils.GetHumanRegion(region)})",
                    Fields = [new EmbedFieldProperties{ Name = $"Clan hasn't played any battles in S{latestSeason}" }]
                });

                return;
            }

            foreach (var rating in data.Clan.ClanView.WowsLadder.Ratings.FindAll(team => team.SeasonNumber == latestSeason))
            {
                clan.Teams.Add(new TeamDto
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
                    Stage = rating.Stage != null ? new StageDto(rating.Stage.Type, rating.Stage.Progress) : null
                });
            }

            clan.Teams.Sort((a, b) => (int)a.TeamNumber - (int)b.TeamNumber);

            clan.Stage = data.Clan.ClanView.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)?.Stage != null
                ? new StageDto(
                    data.Clan.ClanView.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Type,
                    data.Clan.ClanView.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Progress
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

            var response = await ApiClient.Instance.PostAsync("http://localhost:3000/ratings", content);

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(imageBytes);
            
            await deferredMessage.Interaction.SendFollowupMessageAsync(
                new InteractionMessageProperties()
                    .WithAttachments([new AttachmentProperties($"{clanId}.png", stream)])
            );
        }
        catch (Exception ex)
        {
            await Program.LogError(ex);
            await deferredMessage.EditAsync("❌ LogError fetching clan data from API.");
        }
    }

    private record ClanDto
    {
        public string Name { get; init; }
        public string Color { get; init; }
        public League League { get; init; }
        public Division Division { get; init; }
        public int DivisionRating { get; init; }
        public int GlobalRank { get; set; }
        public int RegionRank { get; set; }
        public List<TeamDto> Teams { get; } = [];
        public StageDto? Stage { get; set; }
    }

    private record TeamDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required TeamNumber TeamNumber { get; init; }
        public string Color { get; init; }
        public required int Battles { get; init; }
        public required double WinRate { get; init; }
        public required double SuccessFactor { get; init; }
        public required League League { get; init; }
        public required Division Division { get; init; }
        public required int DivisionRating { get; init; }
        public required StageDto? Stage { get; init; }
    }

    private record StageDto(StageType type, string[] progress)
    {
        public StageType Type { get; } = type;
        public string[] Progress { get; } = progress;
    }
}