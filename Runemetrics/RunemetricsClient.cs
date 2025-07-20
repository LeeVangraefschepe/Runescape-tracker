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
            int updatedUserCount = 0;

            // Query users
            var users = await db.Users
                .Include(u => u.Fetches)
                .ThenInclude(f => f.Skills)
                .ThenInclude(s => s.SkillXpEntries)
                .ToListAsync();

            foreach (var user in users)
            {
                // Fetch user data from Runescape API
                var result = await GetProfileAsync(user.Name);

                // Get latest for all skills
                var latestFetches = user.Fetches
                    .OrderByDescending(f => f.FetchTime);

                // Get all latest skill values
                Dictionary<Skill, long> storedSkillValues = new Dictionary<Skill, long>();
                foreach (Skill skillEnum in Enum.GetValues(typeof(Skill)))
                {
                    bool found = false;
                    foreach (var latestFetch in latestFetches)
                    {
                        foreach (var skillXpEntry in latestFetch.Skills.SkillXpEntries)
                        {
                            if (skillXpEntry.Skill != skillEnum) continue;

                            storedSkillValues.Add(skillEnum, skillXpEntry.Xp);
                            found = true;
                            break;
                        }

                        if (found) break;
                    }
                }

                var fetch = new Fetch
                {
                    UserId = user.Id,
                    FetchTime = DateTime.UtcNow
                };

                var skills = new Skills
                {
                    Fetch = fetch,
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

                    // Skill has different value now add it
                    if (storedSkillValues[skillEntry.Skill] != skillEntry.Xp)
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

                if (skillEntries.Count == 0) continue;

                ++updatedUserCount;

                skills.SkillXpEntries = skillEntries;

                db.Skills.Add(skills);
                await db.SaveChangesAsync();
            }

            return updatedUserCount;
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