using HotelAvailability.Api.Models.Requests;

namespace HotelAvailability.Api.Validation;

public sealed record ValidationError(string PropertyName, string ErrorMessage);

public sealed class SearchAvailabilityRequestValidator(TimeProvider timeProvider)
{
    public IReadOnlyList<ValidationError> Validate(SearchAvailabilityRequest request)
    {
        var errors = new List<ValidationError>();
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (request.HotelId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.HotelId), "HotelId is required."));
        }

        if (request.CheckIn < today)
        {
            errors.Add(new ValidationError(nameof(request.CheckIn), "Check-in date cannot be in the past."));
        }
        else if (request.CheckIn > today.AddYears(1))
        {
            errors.Add(new ValidationError(nameof(request.CheckIn), "Check-in date cannot be more than a year in advance."));
        }

        if (request.CheckOut <= request.CheckIn)
        {
            errors.Add(new ValidationError(nameof(request.CheckOut), "Check-out date must be after check-in date."));
        }
        else if (request.CheckOut > request.CheckIn.AddMonths(1))
        {
            errors.Add(new ValidationError(nameof(request.CheckOut), "Stay length cannot exceed one month."));
        }

        if (request.Rooms < 1)
        {
            errors.Add(new ValidationError(nameof(request.Rooms), "At least one room is required."));
        }

        if (request.Adults < 1)
        {
            errors.Add(new ValidationError(nameof(request.Adults), "At least one adult is required."));
        }

        if (request.ChildrenAges is not null)
        {
            for (var i = 0; i < request.ChildrenAges.Count; i++)
            {
                var age = request.ChildrenAges[i];
                if (age is < 0 or > 17)
                {
                    errors.Add(new ValidationError(
                        $"{nameof(request.ChildrenAges)}[{i}]",
                        "Child age must be between 0 and 17."));
                }
            }
        }

        return errors;
    }
}
