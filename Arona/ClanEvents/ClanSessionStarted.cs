namespace Arona.ClanEvents;

public sealed record ClanSessionStarted(
    int ClanId,
    string ClanName,
    string ClanTag
);