using beveikZinojau.lt.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace beveikZinojau.lt.Services.History.HistoryGameRecords
{
    public record HistorySessionCreateRequest(
        int QuestionCount,
        string Scope,        // "Pasaulis" or "All"
        string Period,       // "Senovė", "Viduramžiai", "Naujieji amžiai", "Modernybė", or "All"
        DifficultyLevel Difficulty
    );
}
