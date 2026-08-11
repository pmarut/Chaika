namespace HotelAvailability.Api.Models.Common;

public enum MealPlanType
{
    RoomOnly,
    Breakfast,
    HalfBoard,
    FullBoard,
    AllInclusive
}

public sealed record MealPlan(MealPlanType Type, string? Description);
