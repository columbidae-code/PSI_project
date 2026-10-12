

namespace beveikZinojau.lt.Data
{
    public class AppGame 
    {

        public long Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }
        public string gameMode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime EndedAt { get; set; } = DateTime.UtcNow;
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? FinalScore { get; set; }

    }
}