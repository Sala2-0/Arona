using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Utility;
using Arona.ApiModels;

namespace Arona.Autocomplete;

internal class PlayerAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context
    )
    {
        var input = option.Value ?? string.Empty;

        string? region = "eu";
        if (input.Contains(' '))
        {
            var split = input.Split(' ');
            region = split[0].ToLower();
            input = split[1];

            region = region switch
            {
                "eu" => "eu",
                "asia" => "asia",
                "na" => "com",
                _ => null
            };
        }

        if (string.IsNullOrEmpty(input) || input.Length < 2 || region == null)
            return [new ApplicationCommandOptionChoiceProperties("Ex: Yurizono_Seia_", "557422466,Yurizono_Seia_,eu")];

        try
        {
            var data = await OfficialApi.Player.GetAsync(input, region);
            var players = data.Select(c => new { AccountId = c.AccountId, Name = c.Nickname, Region = region }).ToList();

            return players
                .Take(8)
                .Select(s =>
                    new ApplicationCommandOptionChoiceProperties($"[{s.Name} ({ClanUtils.GetRegionCode(s.Region)})", $"{s.AccountId},{s.Name},{s.Region}"));
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            return [];
        }
    }
}