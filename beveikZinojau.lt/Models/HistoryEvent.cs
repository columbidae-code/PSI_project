namespace beveikZinojau.lt.Models
{
    public class HistoryEvent
    {
        public string Scope { get; set; }
        public string Period { get; set; }
        public int Year { get; set; }
        public DateTime? Date { get; set; }
    }
}