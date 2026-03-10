using NetCord.Gateway;

namespace Arona.Utility;

internal static class BotUtilities
{
    public static async Task<string> GetBotIconUrl(GatewayClient client)
    {
        var self = await client.Rest.GetCurrentUserAsync();
        return self.GetAvatarUrl()!.ToString();
    }
}
