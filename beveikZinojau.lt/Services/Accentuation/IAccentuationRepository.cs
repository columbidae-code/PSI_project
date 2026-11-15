using beveikZinojau.lt.Models;

namespace beveikZinojau.lt.Services.Accentuation;

public interface IAccentuationRepository
{
    IReadOnlyList<AccentuationWord> GetWords(int level);
}