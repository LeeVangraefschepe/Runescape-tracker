using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Runescape_tracker.Database;
using Runescape_tracker.Runemetrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Runescape_tracker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Read connection string
            var connectionString = config.GetConnectionString("DefaultConnection");

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
            Task webServerTask = Task.Run(() => StartWebServer());

            // Keep application open
            while (true)
            {
                Console.ReadLine();
            }
        }

        static void StartWebServer()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // Configure DB
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            // GET /api/skillvalues?username=...&from=...&to=...
            app.MapGet("/api/skillvalues", async (
                [FromQuery] string username,
                [FromQuery] DateTime? from,
                [FromQuery] DateTime? to) =>
            {
                await using var db = new AppDbContext(dbOptions);

                var user = await db.Users
                    .Include(u => u.Fetches)
                    .ThenInclude(f => f.Skills)
                    .ThenInclude(s => s.SkillXpEntries)
                    .FirstOrDefaultAsync(u => u.Name == username);

                if (user == null)
                    return Results.NotFound("User not found");

                var fetches = user.Fetches
                    .Where(f =>
                        (!from.HasValue || f.FetchTime >= from.Value) &&
                        (!to.HasValue || f.FetchTime <= to.Value))
                    .OrderBy(f => f.FetchTime)
                    .ToList();

                var result = fetches.Select(f => new
                {
                    timestamp = f.FetchTime,
                    skillXp = f.Skills.SkillXpEntries.ToDictionary(x => x.Skill.ToString(), x => x.Xp)
                }).ToList();

                var lastResult = result.Last();

                foreach (Skill skillEnum in Enum.GetValues(typeof(Skill)))
                {
                    var enumKey = skillEnum.ToString();

                    if (lastResult.skillXp.ContainsKey(enumKey)) continue;

                    long value = -1;
                    foreach (var fetch in result)
                    {
                        if (!fetch.skillXp.ContainsKey(enumKey)) continue;

                        value = fetch.skillXp[enumKey];
                    }

                    if (value == -1) continue;

                    lastResult.skillXp.Add(enumKey, value);
                }


                // Make sure that the last timestamp has all the skill values aswell.
                return Results.Ok(result);
            });

            app.UseDefaultFiles(); // for serving static index.html if needed
            app.UseStaticFiles();

            app.Run("http://localhost:999");
        }

    }
}