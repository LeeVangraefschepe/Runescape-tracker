using System.Text.Json.Serialization;

namespace Runescape_tracker.DataStructures
{
    public class PlayerData
    {
        [JsonPropertyName("questsstarted")]
        public int QuestsStarted { get; set; }

        [JsonPropertyName("totalskill")]
        public int TotalSkill { get; set; }

        [JsonPropertyName("questscomplete")]
        public int QuestsComplete { get; set; }

        [JsonPropertyName("questsnotstarted")]
        public int QuestsNotStarted { get; set; }

        [JsonPropertyName("totalxp")]
        public long TotalXp { get; set; }

        [JsonPropertyName("skillvalues")]
        public List<SkillValue> SkillValues { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rank")]
        public string Rank { get; set; }

        [JsonPropertyName("combatlevel")]
        public int CombatLevel { get; set; }
    }

    public class SkillValue
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("xp")]
        public long Xp { get; set; }

        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("id")]
        public Skill Skill { get; set; }
    }

    public enum Skill
    {
        Attack = 0,
        Defence = 1,
        Strength = 2,
        Constitution = 3,
        Ranged = 4,
        Prayer = 5,
        Magic = 6,
        Cooking = 7,
        Woodcutting = 8,
        Fletching = 9,
        Fishing = 10,
        Firemaking = 11,
        Crafting = 12,
        Smithing = 13,
        Mining = 14,
        Herblore = 15,
        Agility = 16,
        Thieving = 17,
        Slayer = 18,
        Farming = 19,
        Runecrafting = 20,
        Hunter = 21,
        Construction = 22,
        Summoning = 23,
        Dungeoneering = 24,
        Divination = 25,
        Invention = 26,
        Archaeology = 27,
        Necromancy = 28
    }
}