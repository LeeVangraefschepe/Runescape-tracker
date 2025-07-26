using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Runescape_tracker.Business;

public class SkillOverviewController
{
    public SkillOverview CreateSkillOverview(List<SkillValues> skillValues)
    {
        var result = new SkillOverview
        {
            SkillValues = skillValues,
        };

        // Get up-to-date values
        var lastSkills = result.SkillValues.Last();
        
        // Reverse skills from latest -> oldest
        var reverseSkills = new List<SkillValues>(result.SkillValues);
        reverseSkills.Reverse();
        
        // Calculate time difference
        Dictionary<TimeDifferences, Dictionary<string, long>> timeDifferences = new();
        foreach (TimeDifferences timeDifference in Enum.GetValues(typeof(TimeDifferences)))
        {
            timeDifferences.Add(timeDifference, GetDifferenceInTimespan(reverseSkills, lastSkills, GetTimeSpanFromEnum(timeDifference)));
        }
        ResortResult(timeDifferences);
        result.SkillGaps = timeDifferences;
        
        return result;
    }
    
    private void ResortResult(Dictionary<TimeDifferences, Dictionary<string, long>> result)
    {
        if (result.Count == 0) return;

        // Get the last record
        var lastRecord = result.Last();

        // Determine key order based on lastRecord SkillXp sorted by value
        var orderedKeys = lastRecord.Value
            .OrderBy(kv => Math.Abs(kv.Value))
            .Select(kv => kv.Key)
            .ToList();
        orderedKeys.Reverse();

        // Reorder all SkillXp dictionaries to match orderedKeys
        var keys = result.Keys.ToList();
        foreach (var key in keys)
        {
            var original = result[key];
            var reordered = new Dictionary<string, long>();

            foreach (var orderedKey in orderedKeys)
            {
                if (original.ContainsKey(orderedKey))
                {
                    reordered[orderedKey] = original[orderedKey];
                }
            }

            result[key] = reordered;
        }
    }
    
    private TimeSpan GetTimeSpanFromEnum(TimeDifferences difference)
    {
        switch (difference)
        {
            case TimeDifferences.hour1: return TimeSpan.FromHours(1);
            case TimeDifferences.hour3: return TimeSpan.FromHours(3);
            case TimeDifferences.hour8: return TimeSpan.FromHours(8);
            case TimeDifferences.hour24: return TimeSpan.FromHours(24);
            case TimeDifferences.day3: return TimeSpan.FromDays(3);
            case TimeDifferences.week1: return TimeSpan.FromDays(7);
            case TimeDifferences.week2: return TimeSpan.FromDays(14);
            case TimeDifferences.month1: return TimeSpan.FromDays(30);
            case TimeDifferences.month3: return TimeSpan.FromDays(90);
            case TimeDifferences.month6: return TimeSpan.FromDays(180);
            case TimeDifferences.year: return TimeSpan.FromDays(365);
        }
        return TimeSpan.Zero;
    }
    
    private Dictionary<string, long> GetDifferenceInTimespan(List<SkillValues> reverseSkills, SkillValues lastSkills, TimeSpan timeSpan)
    {
        // Search for older records
        Dictionary<string, long> oldestRecord = new Dictionary<string, long>();
        foreach (var skillValue in reverseSkills)
        {
            // Check if time range has been exceeded
            if (DateTime.Now - skillValue.Timestamp > timeSpan) break;
            
            // Add/overwrite these records
            foreach (var skill in skillValue.SkillXp)
            {
                oldestRecord[skill.Key] = skill.Value;
            }
        }
        
        // Get the differences between these records
        Dictionary<string, long> differences = new Dictionary<string, long>();
        foreach (var skill in lastSkills.SkillXp)
        {
            // Check if there is a record from this skill
            if (!oldestRecord.TryGetValue(skill.Key, out var oldXpValue)) continue;
            var latestXpValue = lastSkills.SkillXp[skill.Key];
            
            // Check if there is a change in value
            if (oldXpValue == latestXpValue) continue;
            
            // Calculate gain
            differences[skill.Key] = latestXpValue - oldXpValue;
        }
        
        return differences;
    }
}

public class SkillOverview
{
    public List<SkillValues> SkillValues { get; set; } = new();
    public Dictionary<TimeDifferences, Dictionary<string, long>> SkillGaps { get; set; } = new();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TimeDifferences
{
    hour1,
    hour3,
    hour8,
    hour24,
    day3,
    week1,
    week2,
    month1,
    month3,
    month6,
    year
}