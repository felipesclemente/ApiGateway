using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using OcelotGateway.Exceptions;
using Serilog;
using Serilog.Events;

namespace OcelotGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(path: "Logs/OcelotGatewayLogs.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Configuration.AddJsonFile("ocelot.json");

                var keyRingPath = Environment.GetEnvironmentVariable("KEY_RING") ?? 
                    throw new OcelotGatewayException("Não foi possível carregar o path do key ring.");
                
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
                    .SetApplicationName("FastFoodTech");

                var authenticationProviderKey = CookieAuthenticationDefaults.AuthenticationScheme;
                builder.Services.AddAuthentication(authenticationProviderKey).AddCookie();
                builder.Services.AddAuthorization();
                builder.Services.AddOcelot(builder.Configuration);

                builder.Services.AddOpenApi();

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                }

                app.UseHttpsRedirection();
                app.UseAuthentication();
                app.UseAuthorization();

                app.UseOcelot().Wait();
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal($"O Ocelot Gateway encerrou inesperadamente. Exception: {ex.GetType()}. Message: {ex.Message}.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
