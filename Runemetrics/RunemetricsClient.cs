using Microsoft.EntityFrameworkCore;
using Runescape_tracker.Database;
using System.Text.Json;

namespace Runescape_tracker.Runemetrics
{
    public class RuneMetricsClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly AppDbContext db;

        public RuneMetricsClient(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<int> FetchAPI()
        {
            // Prepare potential fetch data
            List<Skills> newRecords = new List<Skills>();
            var fetch = new Fetch
            {
                FetchTime = DateTime.UtcNow
            };

            // Query users
            var users = await db.Users.ToListAsync();
            foreach (var user in users)
            {
                // Fetch user data from Runescape API
                var result = await GetProfileAsync(user.Name);

                // Fetch failed continue
                if (result.SkillValues == null) continue;

                // Query Fetches -> Skills -> Xp on Username
                var userSkills = await db.Skills
                    .Include(s => s.SkillXps)
                    .Include(s => s.Fetch)
                    .Where(s => s.UserId == user.Id)
                    .OrderByDescending(s => s.Fetch.FetchTime)
                    .ToListAsync();

                // Get all latest skill values
                Dictionary<Skill, long> storedSkillValues = new Dictionary<Skill, long>();
                foreach (Skill skillEnum in Enum.GetValues(typeof(Skill)))
                {
                    bool found = false;
                    foreach (var userSkillsRecord in userSkills)
                    {
                        foreach (var skillXpEntry in userSkillsRecord.SkillXps)
                        {
                            if (skillXpEntry.Skill != skillEnum) continue;

                            storedSkillValues.Add(skillEnum, skillXpEntry.Xp);
                            found = true;
                            break;
                        }

                        if (found) break;
                    }
                }

                var skills = new Skills
                {
                    Fetch = fetch,
                    UserId = user.Id,
                    TotalXp = result.TotalXp,
                };

                var skillEntries = new List<SkillXp>();

                foreach (var skillEntry in result.SkillValues)
                {
                    // Skill is not stored add it
                    if (!storedSkillValues.ContainsKey(skillEntry.Skill))
                    {
                        skillEntries.Add(new SkillXp
                        {
                            Skill = skillEntry.Skill,
                            Skills = skills,
                            Xp = skillEntry.Xp,
                            Rank = skillEntry.Rank,
                        });
                        continue;
                    }

                    // New skill value is larger (xp can only go up)
                    if (storedSkillValues[skillEntry.Skill] < skillEntry.Xp)
                    {
                        skillEntries.Add(new SkillXp
                        {
                            Skill = skillEntry.Skill,
                            Skills = skills,
                            Xp = skillEntry.Xp,
                            Rank = skillEntry.Rank,
                        });
                        continue;
                    }
                }

                // Assign all xp changes
                skills.SkillXps = skillEntries;

                // Add to new records
                newRecords.Add(skills);
            }

            // Check if any changes
            bool changes = false;
            foreach (var record in newRecords)
            {
                if (record.SkillXps.Count == 0) continue;
                changes = true;
                break;
            }
            if (!changes) return 0;

            // Apply new data towards database
            try
            {
                db.Skills.AddRange(newRecords);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("DB Save Error: " + ex.InnerException?.Message ?? ex.Message);
                throw;
            }

            return newRecords.Count;
        }

        public async Task<PlayerData> GetProfileAsync(string username)
        {
            var url = $"https://apps.runescape.com/runemetrics/profile/profile?user={username}&activities=0";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<PlayerData>(json);

            return profile;
        }
    }
}