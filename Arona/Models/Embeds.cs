using System.Globalization;
using NetCord;
using NetCord.Rest;
using Arona.ApiModels;
using Arona.Utility;
using static Arona.Utility.ClanUtils;

namespace Arona.Models;

internal abstract class Base
{
    public required string IconUrl { get; init; }
    public abstract EmbedProperties CreateEmbed();
}

internal class SessionEmbed : Base
{
    public required string ClanFullName { get; init; } // [TAGG] Namn
    public required string Date { get; init; }
    public required int BattlesCount { get; init; }
    public required int WinsCount { get; init; }
    public required int Points { get; init; }

    public override EmbedProperties CreateEmbed() =>
        new()
        {
            Author = new EmbedAuthorProperties { Name = "Arona's activity report", IconUrl = IconUrl },
            Title = $"`{ClanFullName}`",
            Color = new Color(Convert.ToInt32(PersonalRatingColors.GetColor((double)WinsCount / BattlesCount * 100), 16)),
            Description =
                $"**Battles played:** {BattlesCount}\n" +
                $"**Win rate:** {Math.Round((double)WinsCount / BattlesCount * 100, 2).ToString(CultureInfo.InvariantCulture) + "%"}\n\n" +

                $"**Points earned:** {Points}\n" +
                $"**Average points:** {Math.Round((double)Points / BattlesCount, 2).ToString(CultureInfo.InvariantCulture)}",
            Footer = new EmbedFooterProperties { Text = Date }
        };
}

/// <summary>
/// Represents the details of a battle, including clan information, rankings, and outcomes.
/// </summary>
/// <remarks>
/// This class provides a structured representation of battle-related data, such as the clan's full name,
/// battle time, team number, global and regional rankings, skill factor, and the outcome of the stage.
/// </remarks>
internal class BattleEmbed : Base
{
    public required string ClanFullName { get; init; }
    public required long BattleTime { get; init; }
    public required ClanUtils.Team TeamNumber { get; init; }
    public required int GlobalRank { get; init; }
    public required int RegionRank { get; init; }
    public required double SuccessFactor { get; init; }
    public required bool IsVictory { get; init; }

    /// <summary>
    /// Change of points after the battle.
    /// </summary>
    /// <remarks>Should be null if clan rating is in stage</remarks>
    public int? PointsDelta { get; set; } = null;

    /// <summary>
    /// Gets the outcome of the stage progress, represented as an integer.
    /// </summary>
    /// <remarks>
    /// Should be null if clan rating is not in stage.
    /// 
    /// <list type="table">
    ///   <item>
    ///     <term>1</term>
    ///     <description>Stage victory</description>
    ///   </item>
    ///   <item>
    ///     <term>0</term>
    ///     <description>Stage defeat</description>
    ///   </item>
    ///   <item>
    ///     <term>-1</term>
    ///     <description>Promoted / Stayed in stage</description>
    ///   </item>
    ///   <item>
    ///     <term>-2</term>
    ///     <description>Demoted / Failed to promote</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public int? StageProgressOutcome { get; set; } = null;

    public required League League { get; init; }
    public required Division Division { get; init; }
    public required int DivisionRating { get; init; }
    public required ClanBase.Stage? Stage { get; init; }

    public override EmbedProperties CreateEmbed()
    {
        var embed = new EmbedProperties
        {
            Author = new EmbedAuthorProperties { Name = "Arona's activity report", IconUrl = IconUrl },
            Title = $"`{ClanFullName}` ({TeamNumber}) finished a battle",
            Color = new Color(Convert.ToInt32(IsVictory ? "54E894" : "EE5353", 16)),
            Fields = [
                new EmbedFieldProperties { Name = "Global rank", Value = $"#{GlobalRank}", Inline = true },
                new EmbedFieldProperties { Name = "Region rank", Value = $"#{RegionRank}", Inline = true },
                new EmbedFieldProperties { Name = "S/F", Value = SuccessFactor.ToString(CultureInfo.InvariantCulture), Inline = true }
            ]
        };

        if (StageProgressOutcome != null)
        {
            string outcomeEmoji = StageProgressOutcome switch
            {
                1 => Emojis.StageProgressVictory,
                0 => Emojis.StageProgressDefeat,
                -1 => Emojis.StagePromoted,
                -2 => Emojis.StageDemoted,
                _ => string.Empty
            };

            embed.Description = $"**Time:** <t:{BattleTime}:f>\n\n" +
                                $"**Outcome:** {(IsVictory ? "Victory" : "Defeat")} {outcomeEmoji}\n\n";

            if (Stage != null)
                embed.Description += $"{GetPromotionType(Stage.Type)} {TargetLeague(Stage.Type)} {TargetDivision(Stage.Type)}";
            else
                embed.Description += $"{League} {Division} ({DivisionRating})";
        }
        else
        {
            embed.Description = $"**Time:** <t:{BattleTime}:f>\n\n" +
                                $"**Outcome:** {(IsVictory ? "Victory" : "Defeat")} {PointsDelta:+0;-0;0}\n\n";

            if (Stage != null)
                embed.Description += $"{GetPromotionType(Stage.Type)} {TargetLeague(Stage.Type)} {TargetDivision(Stage.Type)}";
            else
                embed.Description += $"{League} {Division} ({DivisionRating})";
        }

        return embed;
    }

    private string TargetLeague(StageType type) => type switch
    {
        StageType.Promotion => Stage!.TargetLeague.ToString(),
        StageType.Demotion => League.ToString(),
        _ => "undefined"
    };

    private string TargetDivision(StageType type) => type switch
    {
        StageType.Promotion => Stage!.TargetDivision.ToString(),
        StageType.Demotion => Division.ToString(),
        _ => "undefined"
    };
}

internal class DetailedBattleEmbed : Base
{
    public required LadderBattle Data { get; init; }
    public required bool IsVictory { get; init; }
    public required long BattleTime { get; init; }
    
    public override EmbedProperties CreateEmbed()
    {
        var embed = new EmbedProperties
        {
            Author = new EmbedAuthorProperties { Name = "Arona's activity report", IconUrl = IconUrl },
            Title = $"`[{Data.Teams[0].ClanInfo.Tag}] {Data.Teams[0].ClanInfo.Name}` finished a battle",
            Color = new Color(Convert.ToInt32(IsVictory ? "54E894" : "EE5353", 16)),
            Description = $"**{Data.Teams[0].ClanInfo.Tag}** vs **{Data.Teams[1].ClanInfo.Tag}**\n\n" +
                          $"**Time:** <t:{BattleTime}:f>"
        };

        foreach (var team in Data.Teams)
        {
            var field = new EmbedFieldProperties { Name = $"{team.ClanInfo.Tag} ({team.TeamNumber})", Inline = true };
            var resultEmoji = team.Result == "victory"
                ? Emojis.StageProgressVictory
                : Emojis.StageProgressDefeat;

            if (team.Stage != null)
            {
                if (team.Stage.Progress.Length == 0)
                    field.Value = $"{char.ToUpper(team.Result[0]) + team.Result[1..]} {team.RatingDelta:+0;-0;0}\n\n";
                else
                    field.Value = $"{char.ToUpper(team.Result[0]) + team.Result[1..]} {resultEmoji}\n\n";

                field.Value += $"{GetPromotionType(team.Stage.Type)} {TargetLeague(team)} {TargetDivision(team)}\n\n";
            }
            else
                field.Value = $"{char.ToUpper(team.Result[0]) + team.Result[1..]} {team.RatingDelta:+0;-0;0}\n\n" +
                              $"{team.League} {team.Division} ({team.DivisionRating})\n\n";

            field.Value += "**Lineup:**\n";

            foreach (var player in team.Players)
                field.Value += $"`{player.Name}`\n{player.Ship.Level} {player.Ship.Name}\n\n";

            embed.AddFields(field);
        }

        return embed;
    }

    private static string TargetLeague(LadderBattle.Team team) => team.Stage!.Type switch
    {
        StageType.Promotion => team.Stage.TargetLeague.ToString(),
        StageType.Demotion => team.League.ToString(),
        _ => "undefined"
    };

    private static string TargetDivision(LadderBattle.Team team) => team.Stage!.Type switch
    {
        StageType.Promotion => team.Stage.TargetDivision.ToString(),
        StageType.Demotion => team.Division.ToString(),
        _ => "undefined"
    };
}