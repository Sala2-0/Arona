namespace Arona.Events;

public sealed record ClanBattleSessionEnded
{
    public int ClanId { get; init; }
    public string ClanTag { get; init; }
    public string ClanName { get; init; }
    public int BattlesCount { get; init; }
    public int WinsCount { get; init; }
    public int TotalPoints { get; init; }
    public DateOnly Date { get; init; }
};