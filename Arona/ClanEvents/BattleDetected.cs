using Arona.Models;
using Arona.Models.Api.Clans;

namespace Arona.ClanEvents;

public struct BattleDetected
{
    public required int ClanId { get; init; }
    public required string Region { get; init; }
    public required string ClanTag { get; init; }
    public required string ClanName { get; init; }
    public required long BattleTime { get; init; }
    public required TeamNumber TeamNumber { get; init; }
    public required int GlobalRank { get; init; }
    public required int RegionRank { get; init; }
    public required double SuccessFactor { get; init; }

    public bool IsVictory { get; init; }
    public int? PointsDelta { get; set; }
    public StageProgressOutcome StageProgressOutcome { get; set; }

    public required League League { get; init; }
    public required Division Division { get; init; }
    public required int DivisionRating { get; init; }
    public required Stage? Stage { get; init; }
}
