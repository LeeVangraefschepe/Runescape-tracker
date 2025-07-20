namespace Runescape_tracker.Database
{
    public class Skills
    {
        public int Id { get; set; }
        public int FetchId { get; set; }
        public Fetch Fetch { get; set; } = null!;

        public List<SkillXp> SkillXpEntries { get; set; } = new();

        public long TotalXp { get; set; }
    }

}