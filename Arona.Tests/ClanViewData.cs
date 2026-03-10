using Arona.Models;
using Arona.Models.Api.Clans;

namespace Arona.Tests;

public static class ClanViewData
{
    public static ClanViewRoot PreviousData { get; } = new ClanViewRoot
    {
        ClanView = new ClanView
        {
            Clan = new Clan
            {
                Id = 500140589,
                Color = "#cda4ff",
                Name = "Tepartiet",
                Tag = "SEIA"
            },

            WowsLadder = new WowsLadder
            {
                Division = Division.I,
                DivisionRating = 200,
                LastBattleAt = "2026 - 01 - 25T20:09:32 + 00:00",
                LeadingTeamNumber = TeamNumber.Alpha,
                League = League.Hurricane,
                PlannedPrimeTime = 4,
                PrimeTime = 4,
                SeasonNumber = 32,
                Ratings =
                [
                    new Rating
                    {
                        BattlesCount = 60,
                        WinsCount = 55,
                        Division = Division.I,
                        DivisionRating = 200,
                        League = League.Hurricane,
                        PublicRating = 2400,
                        SeasonNumber = 32,
                        Stage = null,
                        TeamNumber = TeamNumber.Alpha
                    },
                    new Rating
                    {
                        BattlesCount = 46,
                        WinsCount = 34,
                        Division = Division.I,
                        DivisionRating = 99,
                        League = League.Typhoon,
                        PublicRating = 2199,
                        SeasonNumber = 32,
                        Stage = new Stage
                        {
                            Battles = 5,
                            Progress = ["victory", "defeat"],
                            TargetDivision = Division.I,
                            TargetLeague = League.Hurricane,
                            Type = StageType.Promotion,
                            VictoriesRequired = 5
                        },
                        TeamNumber = TeamNumber.Alpha
                    }
                ],
                PublicRating = 2400,
                BattlesCount = 60
            }
        }
    };
    
    public static ClanViewRoot NewData { get; } = new ClanViewRoot
    {
        ClanView = new ClanView
        {
            Clan = new Clan
            {
                Id = 500140589,
                Color = "#cda4ff",
                Name = "Tepartiet",
                Tag = "SEIA"
            },

            WowsLadder = new WowsLadder
            {
                Division = Division.I,
                DivisionRating = 210,
                LastBattleAt = "2026 - 01 - 25T20:09:32 + 00:00",
                LeadingTeamNumber = TeamNumber.Alpha,
                League = League.Hurricane,
                PlannedPrimeTime = 4,
                PrimeTime = 4,
                SeasonNumber = 32,
                Ratings =
                [
                    new Rating
                    {
                        BattlesCount = 61,
                        WinsCount = 56,
                        Division = Division.I,
                        DivisionRating = 210,
                        League = League.Hurricane,
                        PublicRating = 2410,
                        SeasonNumber = 32,
                        Stage = null,
                        TeamNumber = TeamNumber.Alpha
                    },
                    new Rating
                    {
                        BattlesCount = 46,
                        WinsCount = 34,
                        Division = Division.I,
                        DivisionRating = 99,
                        League = League.Typhoon,
                        PublicRating = 2199,
                        SeasonNumber = 32,
                        Stage = new Stage
                        {
                            Battles = 5,
                            Progress = ["victory", "defeat"],
                            TargetDivision = Division.I,
                            TargetLeague = League.Hurricane,
                            Type = StageType.Promotion,
                            VictoriesRequired = 5
                        },
                        TeamNumber = TeamNumber.Alpha
                    }
                ],
                PublicRating = 2410,
                BattlesCount = 61
            }
        }
    };
}