namespace Arona;
using System.Text.Json;
using Config;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Gateway;
using MongoDB.Driver;
using Utility;
using System.Threading.Tasks;
using System.Diagnostics;

internal class Program
{
    // Kastar en TypeInitializationException med JsonException om config.json inte är korrekt konfigurerat
    public static BotConfig Config = JsonSerializer.Deserialize<BotConfig>(BotConfig.GetConfigFilePath())!;
    public static MongoClient DatabaseClient = new(Config.Database);
    public static IMongoCollection<Database.Guild>? GuildCollection { get; private set; }
    public static IMongoCollection<Database.Clan>? ClanCollection { get; private set; }
    public static GatewayClient? Client { get; private set; }
    public static bool UpdateProgress { get; set; } = false;
    public static List<string> ActiveWrites = [];
    
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Debugger.IsAttached
                    ? Config.DevToken // Använd Config.DevToken för utvecklingsläge
                    : Config.Token;
            })
            .AddApplicationCommands();
        
        var host = builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        Client = host.Services.GetRequiredService<GatewayClient>();

        string dbName = Debugger.IsAttached
            ? "Arona_dev"
            : "Arona";

        GuildCollection = DatabaseClient.GetDatabase(dbName)
            .GetCollection<Database.Guild>("Guilds");
        ClanCollection = DatabaseClient.GetDatabase(dbName)
            .GetCollection<Database.Clan>("Clans");

        // Varje minut, hämta API och kolla klan aktiviteter
        Timer clanMonitorTask = new Timer(60000); // 300000
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

    // Väntar på att UpdateClansAsync slutförs om det pågår en
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
}