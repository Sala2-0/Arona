namespace Arona.Utility;
using MongoDB.Driver;
using ApiModels;
using System.Text.Json;
using NetCord;
using NetCord.Rest;
using Commands;
using System.Globalization;

internal class UpdateClan
{
    public static async Task UpdateClansAsync()
    {
        Program.UpdateProgress = true;

        var client = new HttpClient();

        foreach (var dbClan in await Program.ClanCollection!.Find(_ => true).ToListAsync())
        {
            List<string> channelIds = [];

            try
            {
                foreach (var guildId in dbClan.Guilds.ToList())
                {
                    var guild = await Program.GuildCollection!.Find(g => g.Id == guildId).FirstOrDefaultAsync();
                    if (guild != null) channelIds.Add(guild.ChannelId);
                    else dbClan.Guilds.Remove(guildId);
                }

                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var self = await Program.Client!.Rest.GetCurrentUserAsync();
                var botIconUrl = self.GetAvatarUrl()!.ToString();

                if (currentTime >= dbClan.SessionEndTime && dbClan.RecentBattles.Count > 0)
                {
                    string currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                    int wins = 0;
                    int totalPoints = 0;

                    foreach (var battle in dbClan.RecentBattles)
                    {
                        if (battle.GameResult == "Victory") wins++;

                        totalPoints += battle.PointsEarned;
                    }

                    var msg = new SessionMsgProperties
                    {
                        IconUrl = botIconUrl,
                        ClanTag = dbClan.ClanTag,
                        ClanName = dbClan.ClanName,
                        Date = currentDate,
                        TotalBattlesValue = dbClan.RecentBattles.Count.ToString(),
                        WinRateValue = Math.Round((double)wins / dbClan.RecentBattles.Count * 100, 2).ToString(CultureInfo.InvariantCulture) + "%",
                        TotalPointsValue = totalPoints.ToString(),
                        AveragePointsValue = Math.Round((double)totalPoints / dbClan.RecentBattles.Count, 2).ToString(CultureInfo.InvariantCulture)
                    };

                    foreach (var channel in channelIds)
                        await SendMessage(ulong.Parse(channel), CreateEmbed(msg));

                    dbClan.RecentBattles.Clear();
                    dbClan.SessionEndTime = null;
                }

                var res = await client.GetAsync(Clanbase.GetApiUrl(dbClan.Id.ToString(), dbClan.Region));

                var apiClan = JsonSerializer.Deserialize<Clanbase>(await res.Content.ReadAsStringAsync())!;

                int clanId = apiClan.ClanView.Clan.Id;

                int latestSeason = apiClan.ClanView.WowsLadder.SeasonNumber;

                string tag = apiClan.ClanView.Clan.Tag;
                string name = apiClan.ClanView.Clan.Name;

                int? primeTime = apiClan.ClanView.WowsLadder.PrimeTime;
                int? plannedPrimeTime = apiClan.ClanView.WowsLadder.PlannedPrimeTime;

                if (dbClan.PrimeTime.Active == null && primeTime != null)
                {
                    foreach (var guildId in dbClan.Guilds)
                    {
                        var guild = await Program.GuildCollection!.Find(g => g.Id == guildId).FirstOrDefaultAsync();
                        if (guild == null) continue;

                        dbClan.SessionEndTime = GetEndSession(primeTime);

                        await Program.Client.Rest.SendMessageAsync(
                            channelId: ulong.Parse(guild.ChannelId),
                            message: $"`[{tag}] {name} has started playing`"
                        );
                    }
                }
                dbClan.PrimeTime.Active = primeTime;
                dbClan.PrimeTime.Planned = plannedPrimeTime;

                long lastBattleUnix = DateTimeOffset.Parse(apiClan.ClanView.WowsLadder.LastBattleAt).ToUnixTimeSeconds();

                // Hämta rankningar
                Task<string> globalRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId.ToString(), dbClan.Region));
                Task<string> regionRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId.ToString(), dbClan.Region, LadderStructure.ConvertRegion(dbClan.Region)));

                var results = await Task.WhenAll(globalRankTask, regionRankTask);

                var globalRank = JsonSerializer.Deserialize<LadderStructure[]>(results[0]);
                var regionRank = JsonSerializer.Deserialize<LadderStructure[]>(results[1]);

                dbClan.GlobalRank = globalRank!.FirstOrDefault(r => r.Id == clanId)!.Rank;
                dbClan.RegionRank = regionRank!.FirstOrDefault(r => r.Id == clanId)!.Rank;

                // Slag resultat
                foreach (var dbRating in dbClan.Ratings)
                {
                    var apiRating = apiClan.ClanView.WowsLadder.Ratings
                        .FirstOrDefault(r => r.TeamNumber == dbRating.TeamNumber && r.SeasonNumber == latestSeason) ?? null;

                    if (apiRating == null) continue;

                    var msgProp = new MsgProperties
                    {
                        IconUrl = botIconUrl,
                        ClanTag = tag,
                        ClanName = name,
                        LastBattleTime = lastBattleUnix,
                        TeamNumber = apiRating.TeamNumber,
                        GlobalRank = $"#{dbClan.GlobalRank.ToString()}",
                        RegionRank = $"#{dbClan.RegionRank.ToString()}",
                        SuccessFactor = SuccessFactor.Calculate(
                            rating: apiRating.PublicRating,
                            battlesCount: apiRating.BattlesCount,
                            leagueExponent: Ratings.GetLeagueExponent(apiRating.League)
                        ).ToString("0.##", CultureInfo.InvariantCulture)
                    };

                    bool played = false;

                    if (apiRating.Stage != null)
                    {
                        string type = apiRating.Stage.Type;
                        string league = Ratings.GetLeague(apiRating.Stage.TargetLeague - (type == "demotion" ? 1 : 0));
                        string division = Ratings.GetDivision(
                            type == "promotion"
                            ? apiRating.Stage.TargetDivision
                            : apiRating.Division
                        );

                        msgProp.ResultMsg = (
                            type == "promotion"
                            ? "Qualification for"
                            : "Qualification to stay in"
                        ) + $" {league} {division}";

                        if (apiRating.Stage?.Progress.Length == 0 && dbRating.Stage == null)
                        {
                            msgProp.GameResult = type == "promotion"
                                ? "Victory"
                                : "Defeat";

                            msgProp.GameResultMsg = Ratings.GetProgress(apiRating.Stage.Progress);

                            played = true;
                        }

                        else if (apiRating.Stage?.Progress.Length > dbRating.Stage?.Progress.Count)
                        {
                            msgProp.GameResult = apiRating.Stage.Progress.Last() == "victory"
                                ? "Victory"
                                : "Defeat";

                            msgProp.GameResultMsg = Ratings.GetProgress(apiRating.Stage.Progress);

                            played = true;
                        }
                    }

                    else if (dbRating.Stage != null)
                    {
                        var league = Ratings.GetLeague(apiRating.League);
                        var division = Ratings.GetDivision(apiRating.Division);
                        var divisionRating = apiRating.DivisionRating;

                        if (apiRating.League < dbRating.League)
                        {
                            msgProp.GameResult = "Victory";
                            msgProp.ResultMsg = $"Promoted to {league} {division} ({divisionRating})";
                            msgProp.GameResultMsg = "Qualified";
                        }

                        else if (apiRating.League > dbRating.League)
                        {
                            msgProp.GameResult = "Defeat";
                            msgProp.ResultMsg = $"Demoted to {league} {division} ({divisionRating})";
                            msgProp.GameResultMsg = "Failed to qualify";
                        }

                        else if (apiRating.League == dbRating.League && dbRating.Stage.Type == "demotion")
                        {
                            msgProp.GameResult = "Victory";
                            msgProp.ResultMsg = $"Staying in {league} {division} ({divisionRating})";
                            msgProp.GameResultMsg = "Qualified";
                        }

                        else if (apiRating.League == dbRating.League && dbRating.Stage.Type == "promotion")
                        {
                            msgProp.GameResult = "Defeat";
                            msgProp.ResultMsg = $"Demoted to {league} {division} ({divisionRating})";
                            msgProp.GameResultMsg = "Failed to qualify";
                        }

                        played = true;
                    }

                    else if (apiRating.Division != dbRating.Division)
                    {
                        bool promoted = apiRating.Division < dbRating.Division;

                        string league = Ratings.GetLeague(apiRating.League);
                        string division = Ratings.GetDivision(apiRating.Division);
                        int divisionRating = apiRating.DivisionRating;

                        msgProp.GameResult = promoted ? "Victory" : "Defeat";
                        msgProp.GameResultMsg = promoted
                            ? $"+{apiRating.DivisionRating + 100 - dbRating.DivisionRating}"
                            : $"-{dbRating.DivisionRating + 100 - apiRating.DivisionRating}";
                        msgProp.ResultMsg = $"{(promoted ? "Promoted to" : "Demoted to")} {league} {division} ({divisionRating})";

                        played = true;
                    }

                    else if (apiRating.DivisionRating != dbRating.DivisionRating)
                    {
                        bool victory = apiRating.DivisionRating > dbRating.DivisionRating;

                        string league = Ratings.GetLeague(apiRating.League);
                        string division = Ratings.GetDivision(apiRating.Division);
                        int divisionRating = apiRating.DivisionRating;

                        msgProp.GameResult = victory ? "Victory" : "Defeat";
                        msgProp.GameResultMsg = victory
                            ? $"+{apiRating.DivisionRating - dbRating.DivisionRating}"
                            : $"-{dbRating.DivisionRating - apiRating.DivisionRating}";

                        msgProp.ResultMsg = $"{league} {division} ({divisionRating})";

                        played = true;
                    }

                    if (played)
                    {
                        dbClan.RecentBattles.Add(new Database.RecentBattle
                        {
                            BattleTime = lastBattleUnix,
                            GameResult = msgProp.GameResult,
                            PointsEarned = apiRating.PublicRating - dbRating.PublicRating,
                            TeamNumber = apiRating.TeamNumber,
                        });
                        
                        foreach (var channel in channelIds)
                            await SendMessage(ulong.Parse(channel), CreateEmbed(msgProp));
                    }

                    dbRating.TeamNumber = apiRating.TeamNumber;
                    dbRating.League = apiRating.League;
                    dbRating.Division = apiRating.Division;
                    dbRating.DivisionRating = apiRating.DivisionRating;
                    dbRating.PublicRating = apiRating.PublicRating;
                    dbRating.Stage = apiRating.Stage != null ? new Database.Stage
                    {
                        Type = apiRating.Stage.Type,
                        TargetLeague = apiRating.Stage.TargetLeague,
                        TargetDivision = apiRating.Stage.TargetDivision,
                        Progress = apiRating.Stage.Progress.ToList(),
                        Battles = apiRating.Stage.Battles,
                        VictoriesRequired = apiRating.Stage.VictoriesRequired
                    } : null;
                }

                if (dbClan.Guilds.Count == 0)
                    await Program.ClanCollection.DeleteOneAsync(c => c.Id == dbClan.Id);
                else
                    await Program.ClanCollection.ReplaceOneAsync(
                        filter: c => c.Id == dbClan.Id,
                        replacement: dbClan
                    );
            }
            catch (Exception ex)
            {
                Program.Error(ex);
            }
        }

        Program.UpdateProgress = false;
    }

    private class MsgProperties
    {
        public required string IconUrl { get; init; }
        public required string ClanTag { get; init; }
        public required string ClanName { get; init; }
        public required long LastBattleTime { get; init; }
        public required int TeamNumber { get; init; }
        public string GameResult { get; set; } = string.Empty;
        public string GameResultMsg { get; set; } = string.Empty;
        public  string ResultMsg { get; set; } = string.Empty;
        public required string GlobalRank { get; init; }
        public required string RegionRank { get; init; }
        public required string SuccessFactor { get; init; }
    }

    private class SessionMsgProperties
    {
        public required string IconUrl { get; init; }
        public required string ClanTag { get; init; }
        public required string ClanName { get; init; }
        public required string Date { get; init; }
        public required string TotalBattlesValue { get; init; }
        public required string WinRateValue { get; init; }
        public required string TotalPointsValue { get; init; }
        public required string AveragePointsValue { get; init; }
    }

    private static async Task SendMessage(ulong channelId, EmbedProperties embed)
    {
        try
        {
            await Program.Client!.Rest.SendMessageAsync(
                channelId,
                new MessageProperties()
                    .WithEmbeds([embed])
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    private static EmbedProperties CreateEmbed(MsgProperties prop) =>
        new EmbedProperties()
            .WithColor(
                new Color(
                Convert.ToInt32(
                        value: prop.GameResult == "Victory"
                            ? "00FF00"
                            : "FF0000", 
                        fromBase: 16
                    )
                )
            )
            .WithAuthor(
                new EmbedAuthorProperties()
                    .WithName("Arona's activity report")
                    .WithIconUrl(prop.IconUrl)
            )
            .WithTitle($"`[{prop.ClanTag}] {prop.ClanName}` finished a battle")
            .AddFields(
                new EmbedFieldProperties()
                    .WithName("Time")
                    .WithValue($"<t:{prop.LastBattleTime}:f>")
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Team")
                    .WithValue(GetTeamString(prop.TeamNumber))
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName(prop.GameResult)
                    .WithValue(prop.GameResultMsg)
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Result")
                    .WithValue(prop.ResultMsg)
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Global rank")
                    .WithValue(prop.GlobalRank)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("Region rank")
                    .WithValue(prop.RegionRank)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("S/F")
                    .WithValue(prop.SuccessFactor)
                    .WithInline()
            );

    private static EmbedProperties CreateEmbed(SessionMsgProperties prop) =>
        new EmbedProperties()
            .WithColor(
                new Color(Convert.ToInt32("FFEB85", 16))
            )
            .WithAuthor(
                new EmbedAuthorProperties()
                    .WithName("Arona's activity report")
                    .WithIconUrl(prop.IconUrl)
            )
            .WithTitle($"`[{prop.ClanTag}] {prop.ClanName}`")
            .WithDescription($"Clan Battles session [{prop.Date}]")
            .AddFields(
                new EmbedFieldProperties()
                    .WithName("Total battles played")
                    .WithValue(prop.TotalBattlesValue)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("Win rate")
                    .WithValue(prop.WinRateValue)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Total points earned")
                    .WithValue(prop.TotalPointsValue)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("Average points")
                    .WithValue(prop.AveragePointsValue)
                    .WithInline()
            );

    private static string GetTeamString(int teamNumber) => teamNumber switch
    {
        1 => "Alpha",
        2 => "Bravo",
        _ => "Unknown"
    };

    public static long? GetEndSession(int? sessionNum)
    {
        if (sessionNum == null) return null;

        var utcNow = DateTime.UtcNow;

        DateTime endSession = sessionNum switch
        {
            0 or 1 or 2 or 3 => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 16, 0, 0, DateTimeKind.Utc),
            4 or 5 or 6 or 7 => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 21, 30, 0, DateTimeKind.Utc),
            8 or 9 or 10 or 11 => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 4, 0, 0, DateTimeKind.Utc),
            _ => throw new ArgumentOutOfRangeException(nameof(sessionNum), "Unexpected region value")
        };

        if (utcNow >= endSession) return null;
        return ((DateTimeOffset)endSession).ToUnixTimeSeconds();
    }
}
