using Arona.Commands;

namespace Arona.Models.Components;

/// <summary>
/// Data model representing clan battle season statistics for an account.
/// </summary>
/// <remarks>
/// <term>Interaction</term> <see cref="ClanBattleStats.ActivityAsync"/>
/// </remarks>
internal class AccountClanBattleSeasonData(int seasonId, string name, int battlesCount, int winsCount)
{
    public int Id { get; set; } = seasonId;
    public string Name { get; set; } = name;
    public int BattlesCount { get; set; } = battlesCount;
    public int WinsCount { get; set; } = winsCount;
}