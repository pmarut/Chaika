using HotelAvailability.Api.Models.Common;
using HotelAvailability.Api.Models.Domain;

namespace HotelAvailability.Api.Models.Responses;

public sealed record RatePlanDto(
    Guid Id,
    string Name,
    Money TotalPrice,
    CancellationPolicy CancellationPolicy,
    MealPlan? MealPlan)
{
    public static RatePlanDto FromDomain(RatePlan ratePlan) => new(
        ratePlan.Id,
        ratePlan.Name,
        ratePlan.TotalPrice,
        ratePlan.CancellationPolicy,
        ratePlan.MealPlan);
}
