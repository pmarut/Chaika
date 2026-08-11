namespace HotelAvailability.Api.Models.Requests;

public sealed record CreateBookingRequest(
    Guid HotelId,
    Guid RoomId,
    Guid RatePlanId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Rooms,
    int Adults,
    IReadOnlyList<int>? ChildrenAges,
    GuestInfo Guest);
