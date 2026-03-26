using System.Globalization;
using NetCord;
using NetCord.Rest;
using Arona.Models.Api.Clans;
using Arona.Services;
using Arona.Utility;

using static Arona.Utility.ClanUtils;

namespace Arona.Models;

public abstract class Base
{
    public required string IconUrl { get; init; }
    public abstract EmbedProperties CreateEmbed();
}

public class SessionEmbed : Base
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