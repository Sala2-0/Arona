namespace Arona.Models.DataTransfer;

public record BattleResult
{
    public required string ClanTag { get; init; }
    public required string ClanName { get; init; }
    public required bool IsVictory { get; init; }
    public required int? PointsDelta { get; init; }
    public required League League { get; init; }
    public required Division Division { get; init; }
    public required int DivisionRating { get; init; }
    public required ResultStage? Stage { get; init; }
    public bool IsLineupDataAvailable { get; set; } = false;
    
    public record ResultStage(StageType Type, string[] Progress);
}