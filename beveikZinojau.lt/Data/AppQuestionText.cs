

namespace beveikZinojau.lt.Data
{
    public class AppQuestionText
    {

        public long Id { get; set; }
        public AppGame? Game { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Category { get; set; }
        public string? Difficulty { get; set; }

    }
}