using Arona.Models.Components;
using Arona.Commands;
using Arona.Commands.Components;

namespace Arona.Services;

internal static class RecentInteractions
{
    /// <summary>
    /// Temporarily store account clan battle season data related to message IDs for pagination purposes.
    /// </summary>
    /// <remarks>
    /// <term>Interaction</term> <see cref="ClanBattleStats.ActivityAsync"/>
    /// 
    /// <para>
    /// <term>Component</term> <see cref="ButtonModule.AccountSeasonDataAsync"/>
    /// </para>
    /// 
    /// <para>
    /// Used to associate paginated season data with specific
    /// message IDs for component-based navigation.
    /// </para>
    /// <para>
    /// Instance should be cleared or deleted after 30 seconds of component inactivity.
    /// </para>
    /// 
    /// <list type="table">
    ///   <listheader>
    ///     <term>Key</term>
    ///     <description>Value</description>
    ///   </listheader>
    ///   <item>
    ///     <term><see cref="ulong"/></term>
    ///     <description>Message ID</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Dictionary{TKey, TValue}"/></term>
    ///     <description>
    ///     Maps a page index (<see cref="int"/>) to a list of
    ///     <see cref="AccountClanBattleSeasonData"/> objects representing that page’s data.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public static Dictionary<ulong, Dictionary<int, List<AccountClanBattleSeasonData>>> AccountClanBattleSeasonDataInteractions { get; set; } = new();
}