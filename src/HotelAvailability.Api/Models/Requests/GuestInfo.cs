namespace HotelAvailability.Api.Models.Requests;

public sealed record GuestInfo(
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
