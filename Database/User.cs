namespace Runescape_tracker.Database
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // No database field
        public List<Fetch> Fetches { get; set; } = new();
    }
}