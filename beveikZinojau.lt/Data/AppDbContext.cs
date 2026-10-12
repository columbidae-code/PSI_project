
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace beveikZinojau.lt.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppGame> Games => Set<AppGame>();
        public DbSet<AppQuestionText> QuestionTexts => Set<AppQuestionText>();
        public DbSet<AppCategory> Categories => Set<AppCategory>();
        public DbSet<AppGameResults> GameResults => Set<AppGameResults>();
        public DbSet<AppUserStats> UserStats => Set<AppUserStats>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);  

            // One stats row per user and category
            builder.Entity<AppUserStats>()
                .HasIndex(s => new { s.UserId, s.CategoryId })
                .IsUnique();

            // Faster leaderboard and history queries
            builder.Entity<AppGameResults>()
                .HasIndex(r => new { r.CategoryId, r.PlayedAt });
            builder.Entity<AppGameResults>()
                .HasIndex(r => r.UserId);
        }
    }
}