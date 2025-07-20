namespace Runescape_tracker.Database
{
    public class Fetch
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime FetchTime { get; set; }

        // No database field
        public User User { get; set; } = null!;
        public Skills? Skills { get; set; }
    }
}