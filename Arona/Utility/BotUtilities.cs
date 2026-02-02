namespace Arona.Utility;

internal class BotUtilities
{
    public static async Task<string> GetBotIconUrl()
    {
        var self = await Program.Client!.Rest.GetCurrentUserAsync();
        return self.GetAvatarUrl()!.ToString();
    }
}
