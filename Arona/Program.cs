using System.Diagnostics;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.Commands;
using LiteDB;
using Arona.Database;
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

        DB = new LiteDatabase(Path.Combine(AppContext.BaseDirectory, "Arona_DB.db"));
        Collections.Initialize(DB);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Config.GetToken();
                options.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildUsers | GatewayIntents.Guilds;
            })
            .AddApplicationCommands()
            .AddCommands(options => options.Prefix = Debugger.IsAttached ? "?" : "arona?");
        
        var host = builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        Client = host.Services.GetRequiredService<GatewayClient>();

        // Varje minut, hämta API och kolla klan aktiviteter
        Timer clanMonitorTask = new(10000); // 300000
        clanMonitorTask.Elapsed += async (sender, e) =>
        {
            await UpdateClan.UpdateClansAsync();
        };
        clanMonitorTask.AutoReset = true;
        clanMonitorTask.Enabled = true;
        clanMonitorTask.Start();

        await host.RunAsync();
    }

    public static void Error(Exception ex) =>
        Console.WriteLine("Error: " + ex.Message);

    // Väntar på att UpdateClansAsync eller ?dbcopy slutförs om det pågår en
    // Används bara för kommandon som skriver till databasen
    public static async Task WaitForUpdateAsync()
    {
        while (UpdateProgress)
        {
            Console.WriteLine("Waiting for update to finish...");
            
            await Task.Delay(1000);
        }
    }

    // Vänta tills pågående databasskrivning för en guild är klar
    public static async Task WaitForWriteAsync(string guildId)
    {
        while (ActiveWrites.Contains(guildId))
        {
            Console.WriteLine("Waiting for database write to finish...");

            await Task.Delay(1000);
        }
    }

    // Vänta tills alla pågående databasskrivningar är klara
    public static async Task WaitForWriteAsync()
    {
        while (ActiveWrites.Count > 0)
        {
            Console.WriteLine("Waiting for database write to finish...");

            await Task.Delay(1000);
        }
    }
}