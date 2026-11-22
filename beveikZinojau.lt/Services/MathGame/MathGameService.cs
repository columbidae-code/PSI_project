using beveikZinojau.lt.Data;
using beveikZinojau.lt.Models;
using beveikZinojau.lt.Scoring;
using beveikZinojau.lt.Services.Geography.GeoGameRecords;
using beveikZinojau.lt.Services.MathGame.MathEnum;
using beveikZinojau.lt.Services.MathGame.MathRecords;
using beveikZinojau.lt.Services.ServiceHelpers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


namespace beveikZinojau.lt.Services.MathGame
{

    public class MathGameService
    {
        private readonly CacheStore<GeoQuestionSession> sessions;
        private readonly CacheStore<StoredMathQuestion> questions;
        private readonly MathQuestionGenerator generator;
        private readonly ScoringCalculator scoring;
        private readonly StatsTracker statsTracker;

        public MathGameService(IMemoryCache cache, MathQuestionGenerator generator, ScoringCalculator scoring, StatsTracker statsTracker)
        {
            sessions = new(cache, "math-session");
            questions = new(cache, "math-question");

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

            GeoQuestionSession session = new(questionCount: request.QuestionCount, questionTypes: selectedTypes, userId: userId);

            Guid sessionId = sessions.Add(session);

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
            var session = sessions.Get(sessionId);
            if (session is null) return (null, true);

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


                StoredMathQuestion stored = new()
                {
                    SessionId = sessionId,
                    QuestionType = generated.QuestionType,
                    CorrectAnswer = generated.CorrectAnswer
                };
                Guid questionId = questions.Add(stored);

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
            var session = sessions.Get(sessionId);
            if (session is null) return (null, MathGameError.SessionNotFound);

            if (!session.HasPlayer(request.PlayerId))
                return (null, MathGameError.PlayerNotFound);

            var question = questions.Get(request.QuestionId);
            if (question is null) return (null, MathGameError.QuestionNotFound);

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

            MathQuestionRule rule = MathQuestionRules.For(question.QuestionType);

            if (!question.TryAnswer(request.PlayerId, enteredAnswer,
                    out bool isCorrect, out double answerSeconds,rule.TimeLimitSeconds))
                return (null, MathGameError.AlreadyAnswered);


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
            var session = sessions.Get(sessionId);
            if (session is null) return (null, MathGameError.SessionNotFound);

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
