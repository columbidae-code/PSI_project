using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Models
{
    public class MathAnswerRequest
    {
        public Guid QuestionId { get; set; }
        public Guid PlayerId { get; set; }
        public string? Answer { get; set; }
    }
}
