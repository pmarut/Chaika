using HotelAvailability.Api.Models.Requests;
using HotelAvailability.Api.Validation;
using Microsoft.Extensions.Time.Testing;

namespace HotelAvailability.Api.Tests.Validation;

public class SearchAvailabilityRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly Guid SampleHotelId = Guid.NewGuid();

    private readonly SearchAvailabilityRequestValidator _validator =
        new(new FakeTimeProvider(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

    private static SearchAvailabilityRequest ValidRequest(
        DateOnly? checkIn = null,
        DateOnly? checkOut = null,
        int rooms = 1,
        int adults = 1,
        IReadOnlyList<int>? childrenAges = null) => new(
            SampleHotelId,
            checkIn ?? Today.AddDays(10),
            checkOut ?? Today.AddDays(13),
            rooms,
            adults,
            childrenAges);

    [Fact]
    public void Validate_WithValidRequest_ReturnsNoErrors()
    {
        var errors = _validator.Validate(ValidRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenCheckInIsInThePast_ReturnsError()
    {
        var request = ValidRequest(checkIn: Today.AddDays(-1), checkOut: Today.AddDays(2));

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.CheckIn));
    }

    [Fact]
    public void Validate_WhenCheckInIsMoreThanOneYearAhead_ReturnsError()
    {
        var checkIn = Today.AddYears(1).AddDays(1);
        var request = ValidRequest(checkIn: checkIn, checkOut: checkIn.AddDays(2));

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.CheckIn));
    }

    [Fact]
    public void Validate_WhenCheckOutIsNotAfterCheckIn_ReturnsError()
    {
        var checkIn = Today.AddDays(5);
        var request = ValidRequest(checkIn: checkIn, checkOut: checkIn);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.CheckOut));
    }

    [Fact]
    public void Validate_WhenStayIsLongerThanOneMonth_ReturnsError()
    {
        var checkIn = Today.AddDays(5);
        var request = ValidRequest(checkIn: checkIn, checkOut: checkIn.AddMonths(1).AddDays(1));

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.CheckOut));
    }

    [Fact]
    public void Validate_WhenRoomsIsLessThanOne_ReturnsError()
    {
        var request = ValidRequest(rooms: 0);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.Rooms));
    }

    [Fact]
    public void Validate_WhenAdultsIsLessThanOne_ReturnsError()
    {
        var request = ValidRequest(adults: 0);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.Adults));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(18)]
    public void Validate_WhenChildAgeIsOutOfRange_ReturnsError(int invalidAge)
    {
        var request = ValidRequest(childrenAges: [invalidAge]);

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == $"{nameof(SearchAvailabilityRequest.ChildrenAges)}[0]");
    }

    [Fact]
    public void Validate_WhenHotelIdIsEmpty_ReturnsError()
    {
        var request = ValidRequest() with { HotelId = Guid.Empty };

        var errors = _validator.Validate(request);

        Assert.Contains(errors, e => e.PropertyName == nameof(SearchAvailabilityRequest.HotelId));
    }
}
