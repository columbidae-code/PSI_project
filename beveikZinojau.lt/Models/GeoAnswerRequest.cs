namespace beveikZinojau.lt.Models

{
    public class GeoAnswerRequest
    {
        public Guid QuestionId { get; set; }
        public Guid PlayerId { get; set; }
        public int? SelectedOptionIndex { get; set; }
    }
}