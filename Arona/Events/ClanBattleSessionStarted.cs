namespace Arona.Events;

public sealed record ClanBattleSessionStarted(
    int ClanId,
    string ClanName,
    string ClanTag
);