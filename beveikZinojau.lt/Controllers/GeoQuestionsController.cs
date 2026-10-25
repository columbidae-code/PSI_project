using beveikZinojau.lt.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Controllers
{
    [ApiController, Route("api/geoquestions")]
    public class GeoQuestionsController : ControllerBase
    {
        private readonly IMemoryCache cache;


        private static readonly HashSet<string> SupportedQuestionTypes = new()
        {
            "capital",
            "flag",
            "map",
            "outline",
            "neighbour"
        };


        public GeoQuestionsController(IMemoryCache cache)
        {
            this.cache = cache;
        }


        [HttpPost("session")]
        public IActionResult StartSession([FromBody] GeoSessionCreateRequest request)
        {
            if (request.QuestionCount <= 0)
            {
                return BadRequest("Question count must be positive.");
            }

            List<string> selectedTypes = (request.QuestionTypes ?? new())
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Select(type => type.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (selectedTypes.Count == 0)
            {
                return BadRequest("Select at least one question type.");
            }

            if (selectedTypes.Any(type => !SupportedQuestionTypes.Contains(type)))
            {
                return BadRequest("Unknown question type selected.");
            }

            Guid sessionId = Guid.NewGuid();

            GeoQuestionSession session = new(
                questionCount: request.QuestionCount,
                questionTypes: selectedTypes
            );

            cache.Set(
                $"geo-session:{sessionId}",
                session,
                TimeSpan.FromMinutes(30)
            );

            Guid playerId = session.AddPlayer();

            return Ok(new
            {
                SessionId = sessionId,
                QuestionCount = session.QuestionCount,
                QuestionTypes = session.QuestionTypes,
                PlayerId = playerId
            });
        }

        [HttpPost("session/{sessionId:guid}/questions")]
        public IActionResult GetQuestion(Guid sessionId)
        {
            if (!cache.TryGetValue<GeoQuestionSession>(
                $"geo-session:{sessionId}", out var session) || session is null)
            {
                return NotFound("Session not found or expired.");
            }

            List<Country> countries = LoadCountries();

            string? selectedType = null;
            Country? selectedCountry = null;
            List<Country>? eligibleCountries = null;
            int questionNumber = 0;

            string[] types = session.QuestionTypes.ToArray();
            Random.Shared.Shuffle(types);

            foreach (string candidateType in types)
            {
                List<Country> typeCountries = candidateType switch
                {
                    "capital" => countries
                        .Where(c => !string.IsNullOrWhiteSpace(c.Capital))
                        .ToList(),

                    "neighbour" => countries
                        .Where(c => c.Neighbours.Count > 0)
                        .ToList(),

                    "flag" => countries
                        .Where(c => !string.IsNullOrWhiteSpace(c.Flag))
                        .ToList(),

                    "map" => countries
                        .Where(c => !string.IsNullOrWhiteSpace(c.Map))
                        .ToList(),

                    "outline" => countries
                        .Where(c => !string.IsNullOrWhiteSpace(c.Outline))
                        .ToList(),

                    _ => new List<Country>()
                };

                Country[] shuffledCountries = typeCountries.ToArray();
                Random.Shared.Shuffle(shuffledCountries);

                foreach (Country country in shuffledCountries)
                {
                    GeoQuestionKey key = new(
                        countryIso2: country.Iso2,
                        questionType: candidateType
                    );

                    if (!session.TryRecordQuestion(key, out int number))
                        continue;

                    selectedType = candidateType;
                    selectedCountry = country;
                    eligibleCountries = typeCountries;
                    questionNumber = number;
                    break;
                }

                if (selectedCountry is not null)
                    break;
            }

            if (selectedCountry is null ||
                selectedType is null ||
                eligibleCountries is null)
            {
                return Conflict("Round finished or no unused questions remain.");
            }

            string type = selectedType;


            List<Country> optionCountries =
                type == "capital" ? eligibleCountries : countries;

            List<Country> candidates = optionCountries
                .Where(country => country.Iso2 != selectedCountry.Iso2)
                .ToList();

            List<Country> sameContinent = candidates
                .Where(country => country.Continent == selectedCountry.Continent)
                .ToList();

            List<Country> sameRegion = candidates
                .Where(country => country.Region == selectedCountry.Region)
                .ToList();

            string correctAnswer;

            if (type == "capital")
            {
                correctAnswer = selectedCountry.Capital!.Trim();
            }
            else if (type == "neighbour")
            {
                correctAnswer = selectedCountry.Neighbours[
                    Random.Shared.Next(selectedCountry.Neighbours.Count)
                ];
            }
            else
            {
                correctAnswer = selectedCountry.Name.Trim();
            }

            List<string> answers = new() { correctAnswer };

            if (type == "neighbour")
            {
                AddNeighbourOptions(
                    answers, sameRegion, sameContinent,
                    candidates, selectedCountry
                );
            }
            else
            {
                AddAnswerOptions(answers, sameRegion, type);

                if (answers.Count < 4)
                    AddAnswerOptions(answers, sameContinent, type);

                if (answers.Count < 4)
                    AddAnswerOptions(answers, candidates, type);
            }

            if (answers.Count < 4)
            {
                return BadRequest("Not enough distinct answer options.");
            }

            string[] options = answers.ToArray();
            Random.Shared.Shuffle(options);

            string text;
            string? imageUrl;

            if (type == "capital")
            {
                text = $"Valstybė: {selectedCountry.Name}. Kokia jos sostinė?";
                imageUrl = null;
            }
            else if (type == "flag")
            {
                text = "Kuriai šaliai priklauso ši vėliava?";
                imageUrl = selectedCountry.Flag;
            }
            else if (type == "map")
            {
                text = "Kuri šalis pažymėta žemėlapyje?";
                imageUrl = selectedCountry.Map;
            }
            else if (type == "outline")
            {
                text = "Kurios šalies kontūras pavaizduotas?";
                imageUrl = selectedCountry.Outline;
            }
            else
            {
                text = $"Valstybė: {selectedCountry.Name}. Kuri iš šių šalių su ja ribojasi?";
                imageUrl = null;
            }

            Guid questionId = Guid.NewGuid();

            StoredGeoQuestion storedQuestion = new()
            {
                SessionId = sessionId,
                Options = options,
                CorrectOptionIndex = Array.IndexOf(options, correctAnswer)
            };

            cache.Set(
                $"geo-question:{questionId}",
                storedQuestion,
                TimeSpan.FromMinutes(30)
            );

            return Ok(new
            {
                QuestionId = questionId,
                QuestionNumber = questionNumber,
                TotalQuestions = session.QuestionCount,
                QuestionType = type,
                Text = text,
                ImageUrl = imageUrl,
                Options = options
            });
        }




        [HttpPost("session/{sessionId:guid}/answer")]
        public IActionResult CheckAnswer(
            Guid sessionId,
            [FromBody] GeoAnswerRequest request)
        {
            if (!cache.TryGetValue<GeoQuestionSession>(
                $"geo-session:{sessionId}", out var session)
                || session is null)
            {
                return NotFound("Session not found.");
            }

            if (!session.HasPlayer(request.PlayerId))
            {
                return NotFound("Player not found.");
            }

            if (!cache.TryGetValue<StoredGeoQuestion>(
                $"geo-question:{request.QuestionId}", out var question)
                || question is null)
            {
                return NotFound("Question not found.");
            }

            if (question.SessionId != sessionId)
            {
                return BadRequest("Question belongs to another session.");
            }

            if (!request.SelectedOptionIndex.HasValue)
            {
                return BadRequest("Select an answer.");
            }

            int selectedIndex = request.SelectedOptionIndex.Value;

            if (selectedIndex < 0 || selectedIndex >= question.Options.Length)
            {
                return BadRequest("Invalid option.");
            }

            if (!question.TryAnswer(
                request.PlayerId, selectedIndex, out bool isCorrect))
            {
                return Conflict("Player already answered this question.");
            }

            session.RecordAnswer(request.PlayerId, isCorrect);

            return Ok(new
            {
                IsCorrect = isCorrect,
                CorrectOptionIndex = question.CorrectOptionIndex
            });
        }


        [HttpGet("session/{sessionId:guid}/result")]
        public IActionResult GetResult(
            Guid sessionId,
            [FromQuery] Guid playerId)
        {
            if (!cache.TryGetValue<GeoQuestionSession>(
                $"geo-session:{sessionId}", out var session)
                || session is null)
            {
                return NotFound("Session not found.");
            }

            var stats = session.GetPlayerStats(playerId);

            if (stats is null)
                return NotFound("Player not found.");

            var (answered, correct) = stats.Value;

            if (answered < session.QuestionCount)
                return Conflict("Round not finished.");

            double accuracy = Math.Round(
                (double)correct / answered * 100, 1
            );

            return Ok(new
            {
                TotalQuestions = session.QuestionCount,
                CorrectAnswers = correct,
                WrongAnswers = answered - correct,
                AccuracyPercent = accuracy
            });
        }


        private void AddAnswerOptions(List<string> answers, List<Country> candidates, string type)
        {
            Country[] shuffledCandidates = candidates.ToArray();
            Random.Shared.Shuffle(shuffledCandidates);

            foreach (Country candidate in shuffledCandidates)
            {
                if (answers.Count >= 4)
                {
                    return;
                }

                string option;

                if (type == "capital")
                {
                    option = candidate.Capital!.Trim();
                }
                else
                {
                    option = candidate.Name.Trim();
                }

                if (!answers.Contains(option, StringComparer.OrdinalIgnoreCase))
                {
                    answers.Add(option);
                }
            }
        }

        private void AddNeighbourOptions(List<string> answers, List<Country> sameRegion, List<Country> sameContinent, List<Country> candidates, Country selectedCountry)
        {
            List<Country> nonNeighboursRegional = sameRegion.Where(country => !selectedCountry.Neighbours.Contains(country.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            AddAnswerOptions(answers, nonNeighboursRegional, "neighbour");

            if (answers.Count < 4)
            {
                List<Country> nonNeighboursContinental = sameContinent.Where(country => !selectedCountry.Neighbours.Contains(country.Name, StringComparer.OrdinalIgnoreCase)).ToList();

                AddAnswerOptions(answers, nonNeighboursContinental, "neighbour");

                if (answers.Count < 4)
                {
                    List<Country> nonNeighboursGlobal = candidates.Where(country => !selectedCountry.Neighbours.Contains(country.Name, StringComparer.OrdinalIgnoreCase)).ToList();

                    AddAnswerOptions(answers, nonNeighboursGlobal, "neighbour");
                }
            }

        }

        private List<Country> LoadCountries()
        {
            string projectFolder = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(projectFolder, "Data", "Geography", "countries.json");
            using FileStream stream = System.IO.File.OpenRead(filePath);

            JsonSerializerOptions options = new();
            options.PropertyNameCaseInsensitive = true;

            List<Country>? countries = JsonSerializer.Deserialize<List<Country>>(stream, options);

            return countries ?? new List<Country>();
        }
    }
}