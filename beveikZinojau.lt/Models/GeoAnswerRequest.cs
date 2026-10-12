namespace beveikZinojau.lt.Models

{
    public class GeoAnswerRequest
    {
        public Guid QuestionId { get; set; }
        public int? SelectedOptionIndex { get; set; }
    }
}