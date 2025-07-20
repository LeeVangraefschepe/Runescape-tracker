namespace Runescape_tracker.Database
{
    public class Fetch
    {
        public int Id { get; set; }
        public DateTime FetchTime { get; set; }

        // Navigation property
        public ICollection<Skills> Skills { get; set; }
    }
}