using HotelAvailability.Api.Models.Common;
using HotelAvailability.Api.Models.Domain;
using HotelAvailability.Api.Models.Requests;

namespace HotelAvailability.Api.Services;

public sealed class MockAvailabilityService(IHotelCatalog hotelCatalog) : IAvailabilityService
{
    private const string CurrencyCode = "UAH";

    public async Task<IReadOnlyList<AvailableRoom>> SearchAsync(
        SearchAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var rooms = await hotelCatalog.GetRoomsAsync(request.HotelId, cancellationToken);

        var childrenCount = request.ChildrenAges?.Count ?? 0;
        var adultsPerRoom = CeilDiv(request.Adults, request.Rooms);
        var childrenPerRoom = CeilDiv(childrenCount, request.Rooms);
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        return rooms
            .Where(room => room.MaxAdults >= adultsPerRoom && room.MaxChildren >= childrenPerRoom)
            .Select(room => BuildAvailableRoom(room, request, nights))
            .ToArray();
    }

    // Deterministic mock pricing: base rate scales with room capacity, a small
    // surcharge applies for guests beyond the room count, and the non-refundable
    // plan undercuts the standard (flexible) plan by a fixed ~15%.
    private static AvailableRoom BuildAvailableRoom(Room room, SearchAvailabilityRequest request, int nights)
    {
        var childrenCount = request.ChildrenAges?.Count ?? 0;
        var basePricePerNight = 80m + room.MaxAdults * 20m + room.MaxChildren * 10m;
        var guestFactor = 1m + 0.05m * Math.Max(0, request.Adults - request.Rooms) + 0.03m * childrenCount;

        var standardPrice = Math.Round(basePricePerNight * nights * request.Rooms * guestFactor, 2);
        var nonRefundablePrice = Math.Round(standardPrice * 0.85m, 2);

        var freeCancellationDeadline = new DateTimeOffset(
            request.CheckIn.AddDays(-2).ToDateTime(new TimeOnly(23, 59)),
            TimeSpan.Zero);

        var standardRate = new RatePlan(
            Guid.NewGuid(),
            "Standard Rate",
            new Money(standardPrice, CurrencyCode),
            new CancellationPolicy.FreeCancellationUntil(freeCancellationDeadline),
            new MealPlan(MealPlanType.Breakfast, "Breakfast included"));

        var nonRefundableRate = new RatePlan(
            Guid.NewGuid(),
            "Non-refundable Rate",
            new Money(nonRefundablePrice, CurrencyCode),
            new CancellationPolicy.NonRefundable(),
            MealPlan: null);

        return new AvailableRoom(room, [standardRate, nonRefundableRate]);
    }

    private static int CeilDiv(int value, int divisor) =>
        divisor <= 0 ? value : (value + divisor - 1) / divisor;
}
