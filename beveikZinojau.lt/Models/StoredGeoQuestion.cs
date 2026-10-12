namespace beveikZinojau.lt.Models
{
    public class StoredGeoQuestion
    {
        public string[] Options { get; set; } = Array.Empty<string>();
        public int CorrectOptionIndex { get; set; }
    }
}