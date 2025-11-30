using System.Diagnostics;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.Commands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using LiteDB;
using Arona.Models.DB;
using Arona.Models;
using Arona.Utility;

namespace Arona;

internal class Program
{
    public static LiteDatabase DB { get; set; }
    public static GatewayClient? Client { get; private set; }
    public static bool UpdateProgress { get; set; } = false;
    public static readonly List<string> ActiveWrites = [];
    
    private static async Task Main(string[] args)
    {
        Config.Initialize();

        DB = new LiteDatabase(Path.Combine(AppContext.BaseDirectory, Config.Database));
        Collections.Initialize(DB);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Config.GetToken();
                options.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildUsers | GatewayIntents.Guilds;
            })
            .AddApplicationCommands()
            .AddCommands(options => options.Prefix = Debugger.IsAttached ? "?" : "arona?")
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

        var host = builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        Client = host.Services.GetRequiredService<GatewayClient>();

        // Delete guild from DB when bot is removed
        Client.GuildDelete += (e) =>
        {
            Collections.Guilds.Delete(e.GuildId.ToString());
            return ValueTask.CompletedTask;
        };

        // Varje 5 minut, hämta API och kolla klan aktiviteter
        Timer clanMonitorTask = new(300000); // 300000
        clanMonitorTask.Elapsed += async (_, _) => await UpdateTasks.UpdateClansAsync();
        clanMonitorTask.AutoReset = true;
        clanMonitorTask.Enabled = true;
        clanMonitorTask.Start();

        Timer hurricaneLeaderboardTask = new(1200000);
        hurricaneLeaderboardTask.Elapsed += async (_, _) => await UpdateTasks.UpdateHurricaneLeaderboardAsync();
        hurricaneLeaderboardTask.AutoReset = true;
        hurricaneLeaderboardTask.Enabled = true;
        hurricaneLeaderboardTask.Start();

        // Update leaderboard on startup
        await UpdateTasks.UpdateHurricaneLeaderboardAsync();

        await Emojis.InitializeAsync();
        await host.RunAsync();
    }

    public static async Task Error(Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        Console.WriteLine(ex.StackTrace);

        await Client!.Rest.SendMessageAsync(
            channelId: Config.BackdoorChannel,
            message: $"Bot error: `{ex.Message} \n{ex.StackTrace}`"
        );
    }

    /// <summary>
    /// Väntar på att UpdateClansAsync eller ?dbcopy slutförs om det pågår en
    /// </summary>
    /// <remarks>
    /// Används bara för kommandon eller kod som skriver till databasen
    /// </remarks>
    public static async Task WaitForUpdateAsync()
    {
        while (UpdateProgress)
        {
            Console.WriteLine("Waiting for update to finish...");
            
            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Vänta tills pågående databasskrivning för en guild är klar
    /// </summary>
    /// <param name="guildId">
    /// Guild ID att vänta på
    /// </param>
    public static async Task WaitForWriteAsync(string guildId)
    {
        while (ActiveWrites.Contains(guildId))
        {
            Console.WriteLine("Waiting for database write to finish...");

            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Vänta tills alla pågående databasskrivningar är klara
    /// </summary>
    public static async Task WaitForWriteAsync()
    {
        while (ActiveWrites.Count > 0)
        {
            Console.WriteLine("Waiting for database write to finish...");

            await Task.Delay(1000);
        }
    }
}