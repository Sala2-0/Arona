using System.Diagnostics;
using Microsoft.Playwright;

namespace Arona.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (!Debugger.IsAttached)
            {
                if (args.Length >= 2 && args[0] == "--port" && short.TryParse(args[1], out var port))
                {
                    Global.Port = port;
                    builder.WebHost.UseUrls($"http://localhost:{Global.Port}");
                }
                else
                {
                    Console.WriteLine("Error: No port specified to run on");
                    return;
                }
            }

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddSingleton<IBrowser>(sp =>
            {
                var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();

                return playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args =
                    [
                        "--disable-dev-shm-usage",
                        "--no-sandbox"
                    ]
                }).GetAwaiter().GetResult();
            });

            var app = builder.Build();

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                var browser = app.Services.GetRequiredService<IBrowser>();
                browser.CloseAsync().GetAwaiter().GetResult();
            });

            // Configure the HTTP request pipeline.

            app.UseAuthorization();


            app.MapControllers();
            app.UseStaticFiles();

            app.Run();
        }
    }
}
