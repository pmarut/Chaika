using HotelAvailability.Api.Models.Common;

namespace HotelAvailability.Api.Models.Domain;

public sealed record RatePlan(
    Guid Id,
    string Name,
    Money TotalPrice,
    CancellationPolicy CancellationPolicy,
    MealPlan? MealPlan);

public sealed record AvailableRoom(
    Room Room,
    IReadOnlyList<RatePlan> RatePlans);
