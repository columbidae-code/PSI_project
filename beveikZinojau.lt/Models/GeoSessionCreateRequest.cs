
namespace beveikZinojau.lt.Models
{
    public class GeoSessionCreateRequest
    {
        public int QuestionCount { get; set; }

        public List<string> QuestionTypes { get; set; } = new();
    }
}
