using NetCord.Gateway;

namespace Arona.Services;

public interface IErrorService
{
    public Task LogErrorAsync(Exception ex);
}

public class ErrorService(GatewayClient client) : IErrorService
{
    public async Task LogErrorAsync(Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        Console.WriteLine(ex.StackTrace);

        await client.Rest.SendMessageAsync(
            channelId: Config.BackdoorChannel,
            message: $"Bot error: `{ex.Message} \n{ex.StackTrace}`"
        );
    }
}