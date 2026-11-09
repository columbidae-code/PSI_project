using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Data;

using beveikZinojau.lt.Services.Geography;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.MathGame
{
    public enum MathGameError
    {
        None,
        SessionNotFound,
        PlayerNotFound,
        QuestionNotFound,
        WrongSession,
        MissingAnswer,
        InvalidAnswer,
        AlreadyAnswered,
        RoundNotFinished
    }
    public record MathQuestionIssued(
    Guid QuestionId,
    int QuestionNumber,
    int TotalQuestions,
    string Equation,
    double TimeLimitSeconds,
    string Text
);

    public record MathAnswerChecked(
        bool IsCorrect,
        int CorrectAnswer,
        int PointsEarned,
        int TotalPoints
    );

    public class MathGameService
    {
        private readonly IMemoryCache cache;
        private readonly MathQuestionGenerator generator;
        private readonly ScoringCalculator scoring;
        private readonly StatsTracker statsTracker;

        public MathGameService(IMemoryCache cache, MathQuestionGenerator generator, ScoringCalculator scoring, StatsTracker statsTracker)
        {
            this.cache = cache;
            this.generator = generator;
            this.scoring = scoring;
            this.statsTracker = statsTracker;
        }
        public (GeoSessionCreated? Session, string? Error)
            CreateSession(GeoSessionCreateRequest request, string? userId = null)
        {
            if (request.QuestionCount <= 0)
            {
                return (null, "Question count must be positive.");
            }

            List<string> selectedTypes = (request.QuestionTypes ?? new()).Where(type => !string.IsNullOrWhiteSpace(type))
                .Select(type => type.Trim().ToLowerInvariant())
                .Distinct().ToList();
            if (selectedTypes.Count == 0)
            {
                return (null, "Select at least one operation.");
            }
            if (selectedTypes.Any(type => !MathQuestionGenerator.IsSupported(type)))
            {

                           return (null, "Unknown operation selected.");
            }
            Guid sessionId = Guid.NewGuid();

            GeoQuestionSession session = new(questionCount: request.QuestionCount, questionTypes: selectedTypes, userId: userId);

            cache.Set($"math-session:{sessionId}",
                session, TimeSpan.FromMinutes(30));

            Guid playerId = session.AddPlayer();

            return (new GeoSessionCreated(
                sessionId,
                session.QuestionCount,
                session.QuestionTypes,
                playerId), null);
        }

        public (MathQuestionIssued? Question, bool SessionMissing)
            GetQuestion(Guid sessionId)
        {
            if (!cache.TryGetValue<GeoQuestionSession>(
                $"math-session:{sessionId}", out var session) || session is null)
            {
                return (null, true);
            }

            if (session.IsFull)
            {
                return (null, false);
            }

            for (int attempt = 0; attempt < 200; attempt++)
            {
                GeneratedMathQuestion generated = generator.Generate(session.QuestionTypes);

                if (!session.TryRecordQuestion(generated.Key, out int number))
                {
                    if (session.IsFull)
                        break;

                    continue;
                }

                Guid questionId = Guid.NewGuid();

                StoredMathQuestion stored = new()
                {
                    SessionId = sessionId,
                    QuestionType = generated.QuestionType,
                    CorrectAnswer = generated.CorrectAnswer
                };

                cache.Set(
                    $"math-question:{questionId}",
                    stored,
                    TimeSpan.FromMinutes(30)
                );
                MathQuestionIssued question = new(
                    questionId,
                    number,
                    session.QuestionCount,
                    generated.QuestionType,
                    MathQuestionRules.For(generated.QuestionType).TimeLimitSeconds,
                    generated.Text);
                return (question, false);
            }
            return (null, false);
        }



        public (MathAnswerChecked? Answer, MathGameError Error)
            CheckAnswer(Guid sessionId, MathAnswerRequest request)
        {
            if (!cache.TryGetValue<GeoQuestionSession>(
                $"math-session:{sessionId}", out var session)
                || session is null)
            {
                return (null, MathGameError.SessionNotFound);
            }

            if (!session.HasPlayer(request.PlayerId))
                return (null, MathGameError.PlayerNotFound);

            if (!cache.TryGetValue<StoredMathQuestion>(
                $"math-question:{request.QuestionId}", out var question)
                || question is null)
            {
                return (null, MathGameError.QuestionNotFound);
            }

            if (question.SessionId != sessionId)
                return (null, MathGameError.WrongSession);

            if (string.IsNullOrWhiteSpace(request.Answer))
                return (null, MathGameError.MissingAnswer);

            // Turn the typed text into a number
            string typed = request.Answer.Trim().Replace('−', '-');

            if (!int.TryParse(
                typed,
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out int enteredAnswer))
            {
                return (null, MathGameError.InvalidAnswer);
            }

            if (!question.TryAnswer(request.PlayerId, enteredAnswer, out bool isCorrect))
                return (null, MathGameError.AlreadyAnswered);

            // The server measures the time itself
            double answerSeconds = (DateTime.UtcNow - question.ServedAt).TotalSeconds;
            MathQuestionRule rule = MathQuestionRules.For(question.QuestionType);

            int pointsEarned = scoring.Calculate(
                isCorrect,
                rule.Difficulty,
                new AnswerTiming(answerSeconds, rule.TimeLimitSeconds));

            int totalPoints = session.RecordAnswer(
                request.PlayerId, isCorrect, pointsEarned, answerSeconds);

            return (
                new MathAnswerChecked(isCorrect, question.CorrectAnswer, pointsEarned, totalPoints),
                MathGameError.None
            );
        }

        public (GeoGameResult? Result, MathGameError Error)
            GetResult(Guid sessionId, Guid playerId)
        {
            if (!cache.TryGetValue<GeoQuestionSession>(
                $"math-session:{sessionId}", out var session)
                || session is null)
            {
                return (null, MathGameError.SessionNotFound);
            }

            var stats = session.GetPlayerStats(playerId);

            if (stats is null)
                return (null, MathGameError.PlayerNotFound);

            var (answered, correct, points, totalSeconds) = stats.Value;

            if (answered < session.QuestionCount)
                return (null, MathGameError.RoundNotFinished);

            if (session.UserId is not null && session.TryMarkSaved(playerId))
            {
                try
                {
                    int categoryId = statsTracker.GetOrCreateCategoryId(
                        MathQuestionRules.CategoryName);

                    statsTracker.RecordGame(new AppGameResults
                    {
                        UserId = session.UserId,
                        CategoryId = categoryId,
                        Difficulty = session.QuestionTypes.Max(t => MathQuestionRules.For(t).Difficulty),
                        Score = points,
                        QuestionsAnswered = answered,
                        CorrectAnswers = correct,
                        TotalAnswerTimeSeconds = totalSeconds
                    });
                }
                catch
                {
                    // Let the next request try saving again
                    session.UnmarkSaved(playerId);
                    throw;
                }
            }

            double accuracy = Math.Round(
                (double)correct / answered * 100, 1
            );

            GeoGameResult result = new(
                session.QuestionCount,
                correct,
                answered - correct,
                accuracy,
                points,
                session.UserId is not null
            );

            return (result, MathGameError.None);
        }
    }
}
