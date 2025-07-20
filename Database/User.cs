namespace Runescape_tracker.Database
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation property
        public ICollection<Skills> Skills { get; set; }
    }
}