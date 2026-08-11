namespace HotelAvailability.Api.Models.Requests;

public sealed record SearchAvailabilityRequest(
    Guid HotelId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Rooms,
    int Adults,
    IReadOnlyList<int>? ChildrenAges); // null/empty = no children
