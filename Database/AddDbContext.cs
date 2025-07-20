using Microsoft.EntityFrameworkCore;

namespace Runescape_tracker.Database
{
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

            // User → Skill
            modelBuilder.Entity<Skills>()
                .HasOne(s => s.User)
                .WithMany(u => u.Skills)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Fetch → Skill
            modelBuilder.Entity<Skills>()
                .HasOne(s => s.Fetch)
                .WithMany(f => f.Skills)
                .HasForeignKey(s => s.FetchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Skill → SkillXp
            modelBuilder.Entity<SkillXp>()
                .HasOne(xp => xp.Skills)
                .WithMany(s => s.SkillXps)
                .HasForeignKey(xp => xp.SkillsId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}