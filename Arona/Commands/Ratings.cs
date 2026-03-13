using System.Text.Json;
using System.Text.Json.Serialization;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Models.Api.Clans;
using Arona.Services.Message;
using Arona.Utility;
using Arona.Models;
using Arona.Services;
using NetCord.Gateway;
using Guild = Arona.Models.DB.Guild;

namespace Arona.Commands;

public class Ratings(GatewayClient gatewayClient, ErrorService errorService, IApiService apiService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ratings", "Get detailed information about a clans current ratings on current CB season")]
    public async Task RatingsAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanIdAndRegion
    )
    {
        var deferredMessage = await DeferredMessage.CreateAsync(Context.Interaction);

        Guild.Exists(Context.Interaction);

        var self = await gatewayClient.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        string[] split = clanIdAndRegion.Split(',');
        string region = split[1];
        int clanId = int.Parse(split[0]);

        var clan = await new ClanViewQuery(apiService.HttpClient)
            .GetAsync(new ClanViewRequest(region, clanId))
            .IgnoreRedundantFields();
        var clanRank = await new LadderStructureByClanQuery(apiService.HttpClient)
            .GetRegionAndGlobalRankAsync(clanId, region);

        try
        {
            int latestSeason = clan.WowsLadder.SeasonNumber;
            var leadingTeamNumber = clan.WowsLadder.LeadingTeamNumber;

            var clanDto = new ClanDto
            {
                Name = $"[{clan.Clan.Tag}] {clan.Clan.Name}",
                League = clan.WowsLadder.League,
                Division = clan.WowsLadder.Division,
                DivisionRating = clan.WowsLadder.DivisionRating,
                Color = clan.Clan.Color,
            };

            if (!clan.WowsLadder.Ratings.Exists(team => team.SeasonNumber == latestSeason))
            {
                await deferredMessage.EditAsync(new MessageProperties().AddEmbeds(new EmbedProperties 
                {
                    Author = new EmbedAuthorProperties { Name = "Arona's intelligence report", IconUrl = botIconUrl },
                    Title = $"`[{clan.Clan.Tag}] {clan.Clan.Name}` ({ClanUtils.GetHumanRegion(region)})",
                    Fields = [new EmbedFieldProperties{ Name = $"Clan hasn't played any battles in S{latestSeason}" }]
                }));

                return;
            }

            foreach (var rating in clan.WowsLadder.Ratings.FindAll(team => team.SeasonNumber == latestSeason))
            {
                clanDto.Teams.Add(new TeamDto
                {
                    TeamNumber = rating.TeamNumber,
                    Color = $"#{ClanUtils.GetLeagueColor(rating.League)}",
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

            clanDto.Teams.Sort((a, b) => (int)a.TeamNumber - (int)b.TeamNumber);

            clanDto.Stage = clan.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)?.Stage != null
                ? new StageDto(
                    clan.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Type,
                    clan.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Progress
                )
                : null;

            // Hämta klanens ranking
            clanDto.GlobalRank = clanRank.Global;
            clanDto.RegionRank = clanRank.Region;

            var response = await apiService.PostToServiceAsync("ratings", JsonSerializer.Serialize(clanDto));
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(imageBytes);
            
            await deferredMessage.Interaction.SendFollowupMessageAsync(
                new InteractionMessageProperties()
                    .WithAttachments([new AttachmentProperties($"{clanId}.png", stream)])
            );
        }
        catch (Exception ex)
        {
            await errorService.PrintErrorAsync(ex, $"Error at {nameof(RatingsAsync)}");
            await errorService.NotifyUserOfErrorAsync(Context.Interaction, ex, deferredMode: true);
        }
    }

    public record ClanDto
    {
        public string Name { get; init; }
        public string Color { get; init; }
        public League League { get; init; }
        public Division Division { get; init; }
        public int DivisionRating { get; init; }
        public int? GlobalRank { get; set; }
        public int? RegionRank { get; set; }
        public List<TeamDto> Teams { get; } = [];
        public StageDto? Stage { get; set; }
    }

    public record TeamDto
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

    public record StageDto(StageType Type, string[] Progress);
}