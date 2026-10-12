namespace beveikZinojau.lt.Models
{
    public class Country
    {
        public string Iso2 { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Capital { get; set; }
        public string Continent { get; set; } = "";
        public string Region { get; set; } = "";
        public List<string> Neighbours { get; set; } = new();
        public string? Flag { get; set; }
        public string? Outline { get; set; }
        public string? Map { get; set; }
    }
}