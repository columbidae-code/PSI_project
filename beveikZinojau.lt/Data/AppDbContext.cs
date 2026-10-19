
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

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
    }
}