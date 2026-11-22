
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppGame> Games { get; set; } = default!;
        public DbSet<AppQuestionText> QuestionTexts { get; set; } = default!;
        public DbSet<AppCategory> Categories { get; set; } = default!;
        public DbSet<AppGameResults> GameResults { get; set; } = default!;
        public DbSet<AppUserStats> UserStats { get; set; } = default!;
        public DbSet<Country> Countries { get; set; } = default!;
        public DbSet<HistoryEvent> HistoryEvents { get; set; } = default!;
        public DbSet<AccentuationEntry> AccentuationEntries { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Country>()
                .HasKey(country => country.Iso2);
        }
    }
}