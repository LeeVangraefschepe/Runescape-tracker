namespace Runescape_tracker.Database
{
    public class Skills
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int FetchId { get; set; }
        public Fetch Fetch { get; set; }

        public long TotalXp { get; set; }

        // Navigation property
        public ICollection<SkillXp> SkillXps { get; set; }
    }
}