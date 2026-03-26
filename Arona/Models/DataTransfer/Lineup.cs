using Arona.Commands;
using Arona.Models.Api.Clans;

namespace Arona.Models.DataTransfer;

public record Lineup
{
    public bool IsVictory { get; init; }
    public Team Ally  { get; init; }
    public Team Opponent { get; init; }

    public Lineup(LadderBattle battleInfo, bool isVictory)
    {
        var allyTeam = battleInfo.Teams[0];
        var opponentTeam = battleInfo.Teams[1];

        IsVictory = isVictory;
        Ally = new Team
        {
            Tag = allyTeam.ClanInfo.Tag,
            Name = allyTeam.ClanInfo.Name,
            TeamNumber = allyTeam.TeamNumber.ToString(),
            League = allyTeam.League,
            Division = allyTeam.Division,
            DivisionRating = allyTeam.DivisionRating,
            RatingDelta = allyTeam.RatingDelta,
            Stage = allyTeam.Stage != null
                ? new Ratings.StageDto(Type: allyTeam.Stage.Type, Progress: allyTeam.Stage.Progress)
                : null,
            Players = allyTeam.Players.ToList()
                .Select(p => new Player(Name: p.Name, Survived: p.Survived,
                    new Ship(Name: p.Ship.Name, Level: p.Ship.Level.ToString())))
                .ToArray()
        };
        Opponent = new Team
        {
            Tag = opponentTeam.ClanInfo.Tag,
            Name = opponentTeam.ClanInfo.Name,
            TeamNumber = opponentTeam.TeamNumber.ToString(),
            League = opponentTeam.League,
            Division = opponentTeam.Division,
            DivisionRating = opponentTeam.DivisionRating,
            RatingDelta = opponentTeam.RatingDelta,
            Stage = opponentTeam.Stage != null
                ? new Ratings.StageDto(Type: opponentTeam.Stage.Type, Progress: opponentTeam.Stage.Progress)
                : null,
            Players = opponentTeam.Players.ToList()
                .Select(p => new Player(Name: p.Name, Survived: p.Survived,
                    new Ship(Name: p.Ship.Name, Level: p.Ship.Level.ToString())))
                .ToArray()
        };
    }

    public record Ship(string Name, string Level);
    
    public record Player(string Name, bool Survived, Ship Ship);

    public record Team
    {
        public required string Tag { get; init; }
        public required string Name { get; init; }
        public required string TeamNumber { get; init; }
        public required League League { get; init; }
        public required Division Division { get; init; }
        public required int DivisionRating { get; init; }
        public required int RatingDelta { get; init; }
        public required Ratings.StageDto? Stage { get; init; }
        public required Player[] Players { get; init; }
    }
}