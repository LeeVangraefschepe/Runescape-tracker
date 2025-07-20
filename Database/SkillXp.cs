using Runescape_tracker.Runemetrics;

namespace Runescape_tracker.Database
{
    public class SkillXp
    {
        public int Id { get; set; }

        public Skill Skill { get; set; }

        public long Xp { get; set; }

        public int Rank { get; set; }

        public int SkillsId { get; set; }
        public Skills Skills { get; set; }
    }
}