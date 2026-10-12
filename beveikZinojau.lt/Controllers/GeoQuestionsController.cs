using beveikZinojau.lt.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Controllers
{
    [ApiController]
    [Route("api/geoquestions")]
    public class GeoQuestionsController : ControllerBase
    {
        private readonly IMemoryCache cache;

        public GeoQuestionsController(IMemoryCache cache)
        {
            this.cache = cache;
        }

        [HttpGet]
        public IActionResult GetQuestion([FromQuery] string type = "capital")
        {
            if (type != "capital" && type != "flag" && type != "map" && type != "outline" && type != "neighbour")
            {
                return BadRequest("Unknown question type.");
            }

            List<Country> countries = LoadCountries();

            List<Country> eligibleCountries;

            if (type == "capital")
            {
                eligibleCountries = countries.Where(country => !string.IsNullOrWhiteSpace(country.Capital)).ToList();
            }
            else if (type == "neighbour")
            {
                eligibleCountries = countries.Where(country => country.Neighbours.Count > 0).ToList();
            }
            else
            {
                eligibleCountries = countries;
            }

            if (eligibleCountries.Count == 0)
            {
                return NotFound("No eligible countries found.");
            }

            Country selectedCountry = eligibleCountries[Random.Shared.Next(eligibleCountries.Count)];

            List<Country> optionCountries;

            if (type == "capital")
            {
                optionCountries = eligibleCountries;
            }
            else
            {
                optionCountries = countries;
            }

            List<Country> candidates = optionCountries.Where(country => country.Iso2 != selectedCountry.Iso2).ToList();

            List<Country> sameContinent = candidates.Where(country => country.Continent == selectedCountry.Continent).ToList();

            List<Country> sameRegion = candidates.Where(country => country.Region == selectedCountry.Region).ToList();

            string correctAnswer;

            if (type == "capital")
            {
                correctAnswer = selectedCountry.Capital!.Trim();
            }
            else if (type == "neighbour")
            {
                correctAnswer = selectedCountry.Neighbours[Random.Shared.Next(selectedCountry.Neighbours.Count)];
            }
            else
            {
                correctAnswer = selectedCountry.Name.Trim();
            }

            List<string> answers = new() { correctAnswer };

            if (type == "neighbour")
            {
                AddNeighbourOptions(answers, sameRegion, sameContinent, candidates, selectedCountry);
            }
            else
            {
                AddAnswerOptions(answers, sameRegion, type);
            }

            if (answers.Count < 4)
            {
                return BadRequest("Not enough distinct answer options.");
            }

            string[] options = answers.ToArray();
            Random.Shared.Shuffle(options);

            Guid questionId = Guid.NewGuid();

            StoredGeoQuestion storedQuestion = new()
            {
                Options = options,
                CorrectOptionIndex = Array.IndexOf(options, correctAnswer)
            };

            cache.Set(
                $"geo-question:{questionId}",
                storedQuestion,
                TimeSpan.FromMinutes(30)
            );


            string text;
            string? imageUrl;

            if (type == "capital")
            {
                text = $"What is the capital of {selectedCountry.Name}?";
                imageUrl = null;
            }
            else if (type == "flag")
            {
                text = "Which country does this flag belong to?";
                imageUrl = selectedCountry.Flag;
            }
            else if (type == "map")
            {
                text = "Which country is highlighted on the map?";
                imageUrl = selectedCountry.Map;
            }
            else if (type == "outline")
            {
                text = "Which country does this outline show?";
                imageUrl = selectedCountry.Outline;
            }
            else
            {
                text = $"Which of these countries shares a land border with {selectedCountry.Name}?";
                imageUrl = null;
            }

            return Ok(new
            {
                QuestionId = questionId,
                Text = text,
                ImageUrl = imageUrl,
                Options = options
            });
        }

        [HttpPost("answer")]
        public IActionResult CheckAnswer([FromBody] GeoAnswerRequest request)
        {
            string key = $"geo-question:{request.QuestionId}";

            if (!cache.TryGetValue<StoredGeoQuestion>(key, out var question) || question == null)
            {
                return NotFound("Question not found or expired.");
            }

            if (request.SelectedOptionIndex == null)
            {
                return BadRequest("Select an answer option.");
            }

            int selectedIndex = request.SelectedOptionIndex.Value;

            if (selectedIndex < 0 || selectedIndex >= question.Options.Length)
            {
                return BadRequest("Invalid answer option.");
            }

            bool isCorrect = selectedIndex == question.CorrectOptionIndex;

            return Ok(new
            {
                IsCorrect = isCorrect
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