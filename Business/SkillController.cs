using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Runescape_tracker.Database;
using Runescape_tracker.Runemetrics;

namespace Runescape_tracker.Business
{
    public class SkillController
    {
        public async Task<List<SkillValues>> GetSkillValues(DbContextOptions<AppDbContext> dbOptions, string username, DateTime? from, DateTime? to)
        {
            // Create db connection
            await using var db = new AppDbContext(dbOptions);

            // Get all users from db
            var user = await db.Users.FirstOrDefaultAsync(u => u.Name == username);

            // No users found
            if (user == null) return new List<SkillValues>();

            // Get all skill values from the requested user in certain time range
            var userSkills = await db.Skills
                    .Include(s => s.SkillXps)
                    .Include(s => s.Fetch)
                    .Where(s => s.UserId == user.Id)
                    .Where(s =>
                        (!from.HasValue || s.Fetch.FetchTime >= from.Value) &&
                        (!to.HasValue || s.Fetch.FetchTime <= to.Value))
                    .OrderBy(s => s.Fetch.FetchTime)
                    .ToListAsync();

            // Convert db request towards C# data
            var result = userSkills.Select(s => new SkillValues
            {
                Timestamp = s.Fetch.FetchTime,
                SkillXp = s.SkillXps.ToDictionary(x => x.Skill.ToString(), x => x.Xp)
            }).ToList();

            // Take reference to last result
            var lastResult = result.Last();

            // Make sure that last result has all the skills (copy over from last skill if not the case)
            foreach (Skill skillEnum in Enum.GetValues(typeof(Skill)))
            {
                var enumKey = skillEnum.ToString();

                // Skip if key is present
                if (lastResult.SkillXp.ContainsKey(enumKey)) continue;

                // Search for key value in other fetches
                long value = -1;
                foreach (var fetch in result)
                {
                    if (!fetch.SkillXp.ContainsKey(enumKey)) continue;

                    value = fetch.SkillXp[enumKey];
                }

                // Not found skip this skill?
                if (value == -1) continue;

                // Add skill record
                lastResult.SkillXp.Add(enumKey, value);
            }

            // Prevent stalled skills to have a overtime increase graph
            for (int i = result.Count - 1; i > 0; --i)
            {
                var record = result[i];
                var previousRecord = result[i - 1];

                foreach (var skill in record.SkillXp)
                {
                    if (previousRecord.SkillXp.ContainsKey(skill.Key)) continue;

                    // Search for closest record before this previous one
                    SkillValues? closestPreviousRecord = null;
                    for (int j = i - 2; j >= 0; --j)
                    {
                        var oldRecord = result[j];
                        if (!oldRecord.SkillXp.ContainsKey(skill.Key)) continue;

                        closestPreviousRecord = oldRecord;
                        break;
                    }

                    // If no result found so be it and skip
                    if (closestPreviousRecord == null) continue;

                    // Also skip if it is the same value
                    var value = closestPreviousRecord.SkillXp[skill.Key];
                    if (value == skill.Value) continue;

                    previousRecord.SkillXp.Add(skill.Key, closestPreviousRecord.SkillXp[skill.Key]);
                }
            }

            // Devide all values towards 10
            foreach (var record in result)
            {
                foreach (var skill in record.SkillXp.Keys)
                {
                    record.SkillXp[skill] /= 10;
                }
            }

            // Resort all dictionary skills
            ResortResult(result);

            return result;
        }

        public async Task<List<SkillValues>> GetSkillDifferences(DbContextOptions<AppDbContext> dbOptions, string username, string otherUsername, DateTime? from, DateTime? to)
        {
            List<SkillValues> skillValues = new List<SkillValues>();

            var user0Task = GetSkillValues(dbOptions, username, from, to);
            var user1Task = GetSkillValues(dbOptions, otherUsername, from, to);

            await Task.WhenAll(user0Task, user1Task);

            var user0Skills = await user0Task;
            var user1Skills = await user1Task;

            List<SkillValues> result = new List<SkillValues>();

            foreach (var skills in user0Skills)
            {
                // Check if both parties have a record
                var matchingSkills = user1Skills.FirstOrDefault(s => s.Timestamp == skills.Timestamp);
                if (matchingSkills == null) continue;

                // Fix missing records in other user
                FixBrokenDifference(skills, matchingSkills, user1Skills);

                // Fix missing records in user
                FixBrokenDifference(matchingSkills, skills, user0Skills);

                // Create new adjusted record
                var newSkillValues = new SkillValues
                {
                    Timestamp = skills.Timestamp,
                    SkillXp = new Dictionary<string, long>()
                };

                // Fill in new record data
                foreach (var skill in skills.SkillXp.Keys.ToList())
                {
                    var original = skills.SkillXp[skill];
                    var other = matchingSkills.SkillXp[skill];
                    var xpResult = original - other;

                    newSkillValues.SkillXp.Add(skill, xpResult);
                }

                result.Add(newSkillValues);
            }

            ResortResult(result);
            return result;
        }

        private void FixBrokenDifference(SkillValues source, SkillValues target, List<SkillValues> allTargetSkills)
        {
            // Get skills that are not present in target records but are in the source ones
            List<string> missingSkills = new List<string>();
            foreach (var skill in source.SkillXp)
            {
                if (target.SkillXp.ContainsKey(skill.Key)) continue;
                missingSkills.Add(skill.Key);
            }

            // Search most recent fetch of the missing skill
            foreach (var missingSkill in missingSkills)
            {
                var allTargetSkillsReverse = allTargetSkills.ToList();
                allTargetSkillsReverse.Reverse();

                bool found = false;

                // Try to match with closest record in the past
                foreach (var targetSkill in allTargetSkillsReverse)
                {
                    // Skip records that are in the future of the fetch
                    if (targetSkill.Timestamp > source.Timestamp) continue;

                    // Skip records that do not contain the missing skill
                    if (!targetSkill.SkillXp.ContainsKey(missingSkill)) continue;

                    // Update target record with missing skill
                    target.SkillXp.Add(missingSkill, targetSkill.SkillXp[missingSkill]);
                    found = true;

                    break;
                }

                if (found) continue;

                // Problem: There are no records in the past of this skill. Taking closest one in the future
                foreach (var targetSkill in allTargetSkills)
                {
                    // Skip records that do not contain the missing skill
                    if (!targetSkill.SkillXp.ContainsKey(missingSkill)) continue;

                    // Update target record with missing skill
                    target.SkillXp.Add(missingSkill, targetSkill.SkillXp[missingSkill]);
                    found = true;

                    break;
                }

                if (found) continue;

                // Problem: There is also no record of this skill in the future. Deleting skill from record
                source.SkillXp.Remove(missingSkill);
            }
        }

        private void ResortResult(List<SkillValues> result)
        {
            if (result == null || result.Count == 0)
                return;

            // Get the last record
            var lastRecord = result.Last();

            // Determine key order based on lastRecord SkillXp sorted by value
            var orderedKeys = lastRecord.SkillXp
                .OrderBy(kv => Math.Abs(kv.Value))
                .Select(kv => kv.Key)
                .ToList();

            // Reorder all SkillXp dictionaries to match orderedKeys
            foreach (var record in result)
            {
                var reordered = new Dictionary<string, long>();

                foreach (var key in orderedKeys)
                {
                    if (!record.SkillXp.ContainsKey(key)) continue;
                    reordered[key] = record.SkillXp[key];
                }

                record.SkillXp = reordered;
            }
        }

    }

    public class SkillValues
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("skillXp")]
        public Dictionary<string, long> SkillXp { get; set; }
    }
}
