namespace HotelAvailability.Api.Validation;

public static class ValidationResultExtensions
{
    public static IDictionary<string, string[]> ToProblemDictionary(this IReadOnlyList<ValidationError> errors) =>
        errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
