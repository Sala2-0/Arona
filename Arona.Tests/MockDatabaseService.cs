using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using LiteDB;

namespace Arona.Tests;

public class MockDatabaseService : LiteDatabase
{
    public MockDatabaseService(Stream stream) : base(stream)
    {
        GetCollection<Guild>("guilds").Insert(new Guild
        {
            Id = Config.GuildId ?? throw new NullReferenceException("Guild ID cannot be null"),
            ChannelId = "1425924148663685172",
            Clans = [500256050],
            Cookies = new Dictionary<int, string>
            {
                [500256050] = "secret_cookie_lol"
            }
        });

        GetCollection<ClanView>("clans").Insert(new ClanView
        {
            Id = 500256050,
            Clan = new Clan
            {
                Id = 500256050,
                Tag = "SEIA",
                Name = "Tepartiet",
                Color = "#cda4ff"
            },
            WowsLadder = new WowsLadder
            {
                PrimeTime = 4,
                PlannedPrimeTime = 4,
                Ratings = [
                    new Rating
                    {
                        TeamNumber = TeamNumber.Alpha,
                        League = League.Hurricane,
                        Division = Division.I,
                        DivisionRating = 980,
                        Stage = null,
                        SeasonNumber = 1,
                        PublicRating = 3180,
                        BattlesCount = 49,
                        WinsCount = 44
                    }
                ],
                SeasonNumber = 1,
                LastBattleAt = "2026-02-08T21:33:30+00:00",
                League = League.Hurricane,
                Division =  Division.I,
                DivisionRating = 980,
                LeadingTeamNumber =  TeamNumber.Alpha,
                PublicRating = 3180,
                BattlesCount = 49,
            },
            ExternalData = new External
            {
                Region = "eu",
                Guilds = [Config.GuildId ?? throw new NullReferenceException("Guild ID cannot be null")]
            }
        });
    }
}