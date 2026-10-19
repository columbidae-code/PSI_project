namespace beveikZinojau.lt.Models
{
    public class GeoQuestionKey : IEquatable<GeoQuestionKey>
    {
        public string CountryIso2 { get; }
        public string QuestionType { get; }

        public GeoQuestionKey(string countryIso2, string questionType)
        {
            CountryIso2 = countryIso2;
            QuestionType = questionType;
        }

        public bool Equals(GeoQuestionKey? other)
        {
            if (other is null)
                return false;

            return CountryIso2 == other.CountryIso2 && QuestionType == other.QuestionType;
        }

        public override bool Equals(object? obj)
        {
            return obj is GeoQuestionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CountryIso2, QuestionType);
        }
    }
}