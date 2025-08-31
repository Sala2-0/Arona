using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;
using Arona.Autocomplete;
using Arona.Utility;

namespace Arona.Commands;

public class Ratings : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ratings", "Get detailed information about a clans current ratings on current CB season")]
    public async Task RatingsAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for",
            AutocompleteProviderType = typeof(ClanAutocomplete))] string clanIdAndRegion)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        using HttpClient client = new HttpClient();
        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        Task<string> generalTask = client.GetStringAsync(Clanbase.GetApiUrl(clanId, region));
        Task<string> globalRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId, region));
        Task<string> regionRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId, region, LadderStructure.ConvertRegion(region)));

        try
        {
            string[] results = await Task.WhenAll(generalTask, globalRankTask, regionRankTask);

            var general = JsonSerializer.Deserialize<Clanbase>(results[0]);

            int latestSeason = general!.ClanView.WowsLadder.SeasonNumber;

            string tag = general.ClanView.Clan.Tag;
            string name = general.ClanView.Clan.Name;

            int leadingTeamNumber = general.ClanView.WowsLadder.LeadingTeamNumber;

            var self = await Program.Client!.Rest.GetCurrentUserAsync();
            var botIconUrl = self.GetAvatarUrl()!.ToString();

            var clan = new Clan
            {
                Name = $"[{tag}] {name}",
                League = general.ClanView.WowsLadder.League,
                Division = general.ClanView.WowsLadder.Division,
                DivisionRating = general.ClanView.WowsLadder.DivisionRating,
                Color = general.ClanView.Clan.Color,
            };

            if (!general.ClanView.WowsLadder.Ratings.Exists(team => team.SeasonNumber == latestSeason))
            {
                await deferredMessage.EditAsync(new EmbedProperties()
                    .WithTitle($"`[{tag}] {name}` ({ClanSearchStructure.GetRegionCode(region)})")
                    .WithAuthor(new EmbedAuthorProperties()
                        .WithName("Arona's intelligence report")
                        .WithIconUrl(botIconUrl)
                    )
                    .AddFields(new EmbedFieldProperties()
                        .WithName("Clan doesn't play clan battles.")
                    )
                );

                return;
            }

            foreach (Rating rating in general.ClanView.WowsLadder.Ratings.FindAll(team => team.SeasonNumber == latestSeason))
            {
                if (rating.SeasonNumber != latestSeason) continue;

                clan.Teams.Add(new Team
                {
                    TeamNumber = rating.TeamNumber,
                    TeamName = rating.TeamNumber == 1 ? "Alpha" : "Bravo",
                    Color = GetLeagueColor(rating.League),
                    Battles = rating.BattlesCount,
                    SuccessFactor = SuccessFactor.Calculate(rating.PublicRating, rating.BattlesCount, GetLeagueExponent(rating.League)),
                    WinRate = Math.Round((double)rating.WinsCount / rating.BattlesCount * 100, 2),
                    League = rating.League,
                    Division = rating.Division,
                    DivisionRating = rating.DivisionRating,
                    Stage = rating.Stage != null ? new Stage(rating.Stage.Type, rating.Stage.Progress) : null
                });
            }

            clan.Teams.Sort((a, b) => string.Compare(a.TeamName, b.TeamName, StringComparison.Ordinal));

            clan.Stage = general.ClanView.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)?.Stage != null
                ? new Stage(
                    general.ClanView.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Type,
                    general.ClanView.WowsLadder.Ratings.Find(t => t.TeamNumber == leadingTeamNumber && t.SeasonNumber == latestSeason)!.Stage!.Progress
                )
                : null;

            // Hämta klanens ranking
            var globalRankDoc = JsonSerializer.Deserialize<LadderStructure[]>(results[1])!;

            foreach (var c in globalRankDoc)
            {
                if (c.Id != int.Parse(clanId)) continue;

                clan.GlobalRank = c.Rank;
                break;
            }

            var regionRankDoc = JsonSerializer.Deserialize<LadderStructure[]>(results[2])!;

            foreach (var c in regionRankDoc)
            {
                if (c.Id != int.Parse(clanId)) continue;

                clan.RegionRank = c.Rank;
                break;
            }

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
            Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
        }
    }

    public static string GetLeague(int league) => league switch
    {
        0 => "Hurricane",
        1 => "Typhoon",
        2 => "Storm",
        3 => "Gale",
        4 => "Squall",
        _ => "undefined",
    };

    public static string GetDivision(int division) => division switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => "undefined",
    };

    public static string GetProgress(string[] arr)
    {
        string[] progress = [" ⬛ ", " ⬛ ", " ⬛ ", " ⬛ ", " ⬛ "];

        for (int p = 0; p < arr.Length; p++)
            progress[p] = arr[p] == "victory" ? " 🟩 " : " 🟥 ";

        string str = "";
        foreach (var result in progress)
            str = string.Concat(str, result);
        return str;
    }

    public static string GetLeagueColor(int league) => league switch
    {
        0 => "#cda4ff", // Hurricane
        1 => "#bee7bd", // Typhoon
        2 => "#e3d6a0", // Storm
        3 => "#cce4e4", // Gale
        4 => "#cc9966", // Squall
        _ => "#ffffff"  // Undefined
    };

    public static double GetLeagueExponent(int league) => league switch
    {
        0 => 1.0, // Hurricane
        1 => 0.8, // Typhoon
        2 => 0.6, // Storm
        3 => 0.4, // Gale
        4 => 0.2, // Squall
        _ => 0    // Undefined
    };


    private class Clan
    {
        public string Name { get; init; }
        public string Color { get; init; }
        public int League { get; init; }
        public int Division { get; init; }
        public int DivisionRating { get; init; }
        public int GlobalRank { get; set; }
        public int RegionRank { get; set; }
        public List<Team> Teams { get; } = [];
        public Stage? Stage { get; set; }
    }

    private class Team
    {
        public required int TeamNumber { get; init; }
        public required string TeamName { get; init; }
        public string Color { get; init; }
        public required int Battles { get; init; }
        public required double WinRate { get; init; }
        public required double SuccessFactor { get; init; }
        public required int League { get; init; }
        public required int Division { get; init; }
        public required int DivisionRating { get; init; }
        public required Stage? Stage { get; init; }
    }

    private class Stage(string type, string[] progress)
    {
        public string Type { get; } = type;
        public string[] Progress { get; } = progress;
    }
}