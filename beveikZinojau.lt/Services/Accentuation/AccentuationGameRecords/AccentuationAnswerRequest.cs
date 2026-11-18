using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.Accentuation.AccentuationGameRecords
{
    public record AccentuationAnswerRequest(
        
        Guid PlayerId,
        
        Guid QuestionId,
        
        int? SelectedPosition,
        
        string? SelectedAccentType
    );
}
