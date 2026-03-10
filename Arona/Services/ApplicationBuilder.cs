using System.Diagnostics;
using Arona.ClanEventHandlers;
using Arona.ClanEvents;
using Arona.Models;
using Arona.Models.DB;
using Arona.Services.Message;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.Commands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Guild = Arona.Models.DB.Guild;
using User = Arona.Models.DB.User;

namespace Arona.Services;

public class ApplicationBuilder
{
    private readonly HostApplicationBuilder _builder;
    
    public HostApplicationBuilder Builder => _builder;

    public ApplicationBuilder(string[] args)
    {
        _builder = Host.CreateApplicationBuilder(args);
        ConfigureServices();
    }
    
    public IHost Build()
    {
        var host = _builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        var client = host.Services.GetRequiredService<GatewayClient>();
        var repository = host.Services.GetRequiredService<IDatabaseRepository>();
        SetupEvents(client, repository);

        return host;
    }
    
    private void ConfigureServices()
    {
        _builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Config.GetToken();
                options.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent |
                                  GatewayIntents.GuildUsers | GatewayIntents.Guilds;
            })
            .AddApplicationCommands()
            .AddCommands(options => options.Prefix = Debugger.IsAttached ? "?" : "arona?")
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>()
            .AddSingleton(_ => new LiteDatabase(Path.Combine(AppContext.BaseDirectory, Config.Database)))
            .AddSingleton<IDatabaseRepository, DatabaseRepository>()
            .AddHttpClient<IApiClient, ApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        
        // Message services
        _builder.Services
            .AddSingleton<IChannelMessageService, ChannelMessageService>()
            .AddSingleton<IPrivateMessageService, PrivateMessageService>();
        
        // Database repository services
        _builder.Services
            .AddSingleton<IDatabaseRepositoryService<User>>(sp =>
                new DatabaseRepositoryService<User>(sp.GetRequiredService<LiteDatabase>(), "users"))
            .AddSingleton<IDatabaseRepositoryService<Guild>>(sp =>
                new DatabaseRepositoryService<Guild>(sp.GetRequiredService<LiteDatabase>(), "guilds"));
        
        // Event handlers
        _builder.Services
            .AddSingleton<IClanEventBus, ClanEventBus>()
            .AddHostedService<DiscordBattleDetectedHandler>()
            .AddHostedService<DiscordSessionStartedHandler>()
            .AddHostedService<DiscordSessionEndedHandler>();
        
        // Background services
        _builder.Services.AddHostedService<ClanUpdateService>();
        
        // Other services
        _builder.Services
            .AddSingleton<IEmojiService, EmojiService>()
            .AddSingleton<IErrorService, ErrorService>();
    }

    private static void SetupEvents(GatewayClient client, IDatabaseRepository repository)
    {
        // Delete guild from DB when bot is removed
        client.GuildDelete += (e) =>
        {
            repository.Guilds.Delete(e.GuildId.ToString());
            return ValueTask.CompletedTask;
        };
        
        client.Ready += _ => client.UpdatePresenceAsync(new PresenceProperties(statusType: UserStatusType.Online)
        {
            Activities = [
                new UserActivityProperties(Config.PrecenseStr, UserActivityType.Playing)
            ],
        });
    }
}