using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Runescape_tracker.Database;
using Runescape_tracker.Runemetrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Runescape_tracker.Business;

namespace Runescape_tracker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string jsonPath = "appsettings.Development.json";
            if (Environment.GetEnvironmentVariable("IS_DOCKER") != null)
                jsonPath = "appsettings.Docker.json";

            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonPath)
                .Build();

            // Read connection string
            var connectionString = config.GetConnectionString("DefaultConnection");
            Console.WriteLine($"Found the following connection string:\n{connectionString}");

            // Configure EF Core
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            // Create DB context
            using var db = new AppDbContext(optionsBuilder.Options);

            // Start background task for fetching every 15m
            _ = Task.Run(async () =>
            {
                var runemetricsClient = new RuneMetricsClient(db);

                while (true)
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now}] Starting fetch...");
                        var amount = await runemetricsClient.FetchAPI();
                        Console.WriteLine($"[{DateTime.Now}] Fetch completed. Detected {amount} user profile changed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Error during fetch: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(15));
                }
            });

            // Start Web Server in background
            Task webServerTask = Task.Run(() => StartWebServer(connectionString));

            // Keep application open
            while (true)
            {
                Console.ReadLine();
            }
        }

        static void StartWebServer(string connectionString)
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            SkillController skillController = new SkillController();

            // GET /api/skillvalues?username=...&from=...&to=...
            app.MapGet("/api/skillvalues", async (
                [FromQuery] string username,
                [FromQuery] DateTime? from,
                [FromQuery] DateTime? to) =>
            {
                var result = await skillController.GetSkillValues(dbOptions, username, from, to);
                return Results.Ok(result);
            });

            // GET /api/skilldifference?username=...&otherUser=...&from=...&to=...
            app.MapGet("/api/skilldifference", async (
                [FromQuery] string username,
                [FromQuery] string otherUser,
                [FromQuery] DateTime? from,
                [FromQuery] DateTime? to) =>
            {
                var result = await skillController.GetSkillDifferences(dbOptions, username, otherUser, from, to);
                return Results.Ok(result);
            });

            app.MapGet("/api/users", async () =>
            {
                // Create db connection
                await using var db = new AppDbContext(dbOptions);

                // Get all users from db
                var users = await db.Users.ToListAsync();

                // Get only the names
                List<string> usernames = new List<string>();
                foreach (var user in users)
                {
                    usernames.Add(user.Name);
                }

                return usernames;
            });

            app.UseDefaultFiles(); // for serving static index.html if needed
            app.UseStaticFiles();

            app.Run("http://+:999");
        }
    }
}