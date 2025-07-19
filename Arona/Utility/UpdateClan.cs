namespace Arona.Utility;
using MongoDB.Driver;
using ApiModels;
using System.Text.Json;
using NetCord;
using NetCord.Rest;
using Commands;
using System.Globalization;
using Commands;

internal class UpdateClan
{
    public static async Task UpdateClansAsync()
    {
        var db = Program.DatabaseClient!.GetDatabase("Arona");
        var guildsCollection = db.GetCollection<Database.Guild>("servers");

        var client = new HttpClient();

        foreach (var guild in await guildsCollection.Find(_ => true).ToListAsync())
        {
            if (guild.Clans == null || guild.Clans.Count == 0) continue;

            List<Task<string>> apiTasks = [];

            foreach (var clanId in guild.Clans.Keys)
            {
                var region = guild.Clans[clanId].Region;
                apiTasks.Add(client.GetStringAsync(Clanbase.GetApiUrl(clanId, region)));
            }

            try
            {
                foreach (var result in await Task.WhenAll(apiTasks))
                {
                    var clan = JsonSerializer.Deserialize<ApiModels.Clanbase>(result);

                    if (clan == null) continue;

                    int clanId = clan.ClanView.Clan.Id;

                    var dbClan = guild.Clans[clanId.ToString()];

                    int latestSeason = clan.ClanView.WowsLadder.SeasonNumber;

                    string tag = clan.ClanView.Clan.Tag;
                    string name = clan.ClanView.Clan.Name;

                    int? primeTime = clan.ClanView.WowsLadder.PrimeTime;
                    int? plannedPrimeTime = clan.ClanView.WowsLadder.PlannedPrimeTime;

                    if (dbClan.PrimeTime.Active == null && primeTime != null)
                    {
                        await Program.Client!.Rest.SendMessageAsync(channelId: ulong.Parse(guild.ChannelId), message: $"`[{tag}] {name} has started playing`");
                    }
                    guild.Clans[clanId.ToString()].PrimeTime.Active = primeTime;
                    guild.Clans[clanId.ToString()].PrimeTime.Planned = plannedPrimeTime;

                    long lastBattleUnix = DateTimeOffset.Parse(clan.ClanView.WowsLadder.LastBattleAt).ToUnixTimeSeconds();

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
                        var apiRating = clan.ClanView.WowsLadder.Ratings
                            .FirstOrDefault(r => r.TeamNumber == dbRating.TeamNumber && r.SeasonNumber == latestSeason) ?? null;

                        if (apiRating == null) continue;

                        var msgProp = new MsgProperties
                        {
                            ClanTag = tag,
                            ClanName = name,
                            LastBattleTime = lastBattleUnix,
                            TeamNumber = apiRating.TeamNumber,
                            GlobalRank = $"#{dbClan.GlobalRank.ToString()}",
                            RegionRank = $"#{dbClan.RegionRank.ToString()}",
                            SuccessFactor = apiRating.BattlesCount >= 20
                                ? (Math.Pow(apiRating.PublicRating, Ratings.GetSuccessFactor(apiRating.League)) / apiRating.BattlesCount).ToString("0.##", CultureInfo.InvariantCulture)
                                : "< 20 battles"
                        };

                        if (apiRating.Stage != null)
                        {
                            string type = apiRating.Stage.Type;
                            string league = Ratings.GetLeague(apiRating.Stage.TargetLeague - (type == "demotion" ? 1 : 0));
                            string division = Ratings.GetDivision(apiRating.Stage.TargetDivision);

                            msgProp.ResultMsg = (type == "promotion"
                                                        ? "Qualification for"
                                                        : "Qualification to stay in")
                                                    + $" {league} {division}";

                            if (apiRating.Stage?.Progress.Length == 0 && dbRating.Stage == null)
                            {
                                msgProp.GameResult = type == "promotion"
                                    ? "Victory"
                                    : "Defeat";

                                msgProp.GameResultMsg = Ratings.GetProgress(apiRating.Stage.Progress);

                                await SendMessage(ulong.Parse(guild.ChannelId), CreateEmbed(msgProp));
                            }

                            else if (apiRating.Stage?.Progress.Length > dbRating.Stage?.Progress.Count)
                            {
                                msgProp.GameResult = apiRating.Stage.Progress.Last() == "victory"
                                    ? "Victory"
                                    : "Defeat";

                                msgProp.GameResultMsg = Ratings.GetProgress(apiRating.Stage.Progress);

                                await SendMessage(ulong.Parse(guild.ChannelId), CreateEmbed(msgProp));
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

                            await SendMessage(ulong.Parse(guild.ChannelId), CreateEmbed(msgProp));
                        }

                        else if (apiRating.Division != dbRating.Division)
                        {
                            var promoted = apiRating.Division < dbRating.Division;

                            var league = Ratings.GetLeague(apiRating.League);
                            var division = Ratings.GetDivision(apiRating.Division);
                            var divisionRating = apiRating.DivisionRating;

                            msgProp.GameResult = promoted ? "Victory" : "Defeat";
                            msgProp.GameResultMsg = promoted
                                ? $"+{apiRating.DivisionRating + 100 - dbRating.DivisionRating}"
                                : $"-{dbRating.DivisionRating + 100 - apiRating.DivisionRating}";
                            msgProp.ResultMsg = $"{(promoted ? "Promoted to" : "Demoted to")} {league} {division} ({divisionRating})";

                            await SendMessage(ulong.Parse(guild.ChannelId), CreateEmbed(msgProp));
                        }

                        else if (apiRating.DivisionRating != dbRating.DivisionRating)
                        {
                            var victory = apiRating.DivisionRating > dbRating.DivisionRating;

                            var league = Ratings.GetLeague(apiRating.League);
                            var division = Ratings.GetDivision(apiRating.Division);
                            var divisionRating = apiRating.DivisionRating;

                            msgProp.GameResult = victory ? "Victory" : "Defeat";
                            msgProp.GameResultMsg = victory
                                ? $"+{apiRating.DivisionRating - dbRating.DivisionRating}"
                                : $"-{dbRating.DivisionRating - apiRating.DivisionRating}";

                            msgProp.ResultMsg = $"{league} {division} ({divisionRating})";

                            await SendMessage(ulong.Parse(guild.ChannelId), CreateEmbed(msgProp));
                        }

                        dbRating.TeamNumber = apiRating.TeamNumber;
                        dbRating.League = apiRating.League;
                        dbRating.Division = apiRating.Division;
                        dbRating.DivisionRating = apiRating.DivisionRating;
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

                    await guildsCollection.ReplaceOneAsync(g => g.Id == guild.Id, guild);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error med hämtning av data: " + ex);
            }
        }
    }

    public class MsgProperties
    {
        public string ClanTag { get; set; }
        public string ClanName { get; set; }
        public long LastBattleTime { get; set; }
        public int TeamNumber { get; set; }
        public string GameResult { get; set; }
        public string GameResultMsg { get; set; }
        public string ResultMsg { get; set; }
        public string GlobalRank { get; set; }
        public string RegionRank { get; set; }
        public string SuccessFactor { get; set; }
    }

    public static async Task SendMessage(ulong channelId, EmbedProperties embed)
    {
        
        try
        {
            await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties()
                .WithEmbeds([embed]));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public static EmbedProperties CreateEmbed(MsgProperties props) =>
        new EmbedProperties()
            .WithColor(new Color(Convert.ToInt32(props.GameResult == "Victory" ? "00FF00" : "FF0000", 16)))
            .WithTitle($"[{props.ClanTag}] {props.ClanName}` finished a battle")
            .AddFields(
                new EmbedFieldProperties()
                    .WithName("Time")
                    .WithValue($"<t:{props.LastBattleTime}:f>")
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Team")
                    .WithValue(GetTeamString(props.TeamNumber))
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName(props.GameResult)
                    .WithValue(props.GameResultMsg)
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Result")
                    .WithValue(props.ResultMsg)
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Global rank")
                    .WithValue(props.GlobalRank)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("Region rank")
                    .WithValue(props.RegionRank)
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("S/F")
                    .WithValue(props.SuccessFactor)
                    .WithInline());

    public static string GetTeamString(int teamNumber) => teamNumber switch
    {
        1 => "Alpha",
        2 => "Bravo",
        _ => "Unknown"
    };
}
