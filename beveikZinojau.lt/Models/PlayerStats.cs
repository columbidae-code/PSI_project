namespace beveikZinojau.lt.Models;

public readonly record struct PlayerStats(
    int Answered,
    int Correct,
    int Points,
    double TotalAnswerTimeSeconds);