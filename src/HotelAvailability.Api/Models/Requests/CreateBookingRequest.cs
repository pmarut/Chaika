namespace HotelAvailability.Api.Models.Requests;

public sealed record CreateBookingRequest(
    Guid HotelId,
    Guid RoomId,
    Guid RatePlanId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Rooms,
    int Adults,
    GuestInfo Guest,
    IReadOnlyList<int>? ChildrenAges = null); // null/empty = no children
