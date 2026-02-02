namespace Arona.ClanEvents;

public sealed record ClanSessionEnded(
    int ClanId,
    string ClanTag,
    string ClanName,
    int BattlesCount,
    int WinsCount,
    int TotalPoints,
    DateOnly Date
);