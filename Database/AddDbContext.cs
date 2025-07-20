namespace Runescape_tracker.Database
{
    using Microsoft.EntityFrameworkCore;

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Fetch> Fetches => Set<Fetch>();
        public DbSet<Skills> Skills => Set<Skills>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Fetch>()
                .HasOne(f => f.User)
                .WithMany(u => u.Fetches)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Fetch>()
                .HasOne(f => f.Skills)
                .WithOne(s => s.Fetch)
                .HasForeignKey<Skills>(s => s.FetchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SkillXp>()
                .HasOne(sx => sx.Skills)
                .WithMany(s => s.SkillXpEntries)
                .HasForeignKey(sx => sx.SkillsId);
        }
    }
}