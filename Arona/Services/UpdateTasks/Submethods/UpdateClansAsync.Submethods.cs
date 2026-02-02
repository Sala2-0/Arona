using System.Security.Authentication;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using NetCord.Rest;

namespace Arona.Services.UpdateTasks.Submethods;

public static class UpdateClansSubmethods
{
    public static void ClearInvalidGuilds(ClanView clan)
    {
        foreach (var guildId in clan.ExternalData.Guilds.ToList())
        {
            var guild = Collections.Guilds.FindOne(g => g.Id == guildId);

            if (guild == null || !guild.Clans.Exists(clanId => clanId == clan.Clan.Id))
                clan.ExternalData.Guilds.Remove(guildId);
        }
    }

    public static bool HasSessionEnded(long currentTime, ClanView clan) =>
        currentTime >= clan.ExternalData.SessionEndTime && clan.ExternalData.RecentBattles.Count > 0;

    public static void CalculateSessionStats(List<RecentBattle> battles, out int wins, out int totalPoints)
    {
        wins = 0;
        totalPoints = 0;

        foreach (var battle in battles)
        {
            if (battle.IsVictory) wins++;
            totalPoints += battle.PointsEarned;
        }
    }

    public static void ResetSessionData(ClanView clan)
    {
        clan.ExternalData.RecentBattles.Clear();
        clan.ExternalData.SessionEndTime = null;

        Collections.Clans.Update(clan);
    }

    public static bool IsNewSeason(int a, int b) => a != b;

    public static void SetNewSeasonData(ClanView dbClan, ClanView apiClan, ClanViewMinimal apiClanMinimal)
    {
        dbClan.WowsLadder = apiClan.WowsLadder;
        dbClan.ExternalData.RecentBattles.Clear();
        dbClan.ExternalData.SessionEndTime = null;
        dbClan.WowsLadder.Ratings.RemoveAll(r => r.SeasonNumber != dbClan.WowsLadder.SeasonNumber);
        dbClan.ExternalData.GlobalRank = apiClanMinimal.GlobalRank;
        dbClan.ExternalData.RegionRank = apiClanMinimal.RegionRank;

        Collections.Clans.Update(dbClan);
    }

    public static bool HasStartedPlaying(int? a, int? b) => a == null && b != null;

    public static bool HasEnteredStage(Stage? stage) => stage is { Progress.Length: 0 };

    public static async Task ValidateCookie(string cookie, string region, int clanId, string tag)
    {
        var cookieValidationData = await AccountInfoSync.GetAsync(cookie, region);

        if (cookieValidationData.ClanId != clanId)
            throw new InvalidCredentialException($"Cookie for clan `{tag}` is invalid: Player is not a member of the clan.");
        if (cookieValidationData.Rank < Role.LineOfficer)
            throw new InvalidCredentialException($"Cookie for clan `{tag}` is invalid: Player is too high ranking.");
    }

    public static void UpdateClanBattleData(ClanView dbClan, ClanView apiClan, ClanViewMinimal minimalData)
    {
        dbClan.ExternalData.GlobalRank = minimalData.GlobalRank;
        dbClan.ExternalData.RegionRank = minimalData.RegionRank;

        dbClan.WowsLadder.PrimeTime = minimalData.PrimeTime;
        dbClan.WowsLadder.PlannedPrimeTime = minimalData.PlannedPrimeTime;
        dbClan.WowsLadder.League = apiClan.WowsLadder.League;
        dbClan.WowsLadder.Division = apiClan.WowsLadder.Division;
        dbClan.WowsLadder.DivisionRating = apiClan.WowsLadder.DivisionRating;
        dbClan.WowsLadder.LastBattleAt = apiClan.WowsLadder.LastBattleAt;
        dbClan.WowsLadder.LeadingTeamNumber = apiClan.WowsLadder.LeadingTeamNumber;
    }
}