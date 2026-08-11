namespace HotelAvailability.Api.Models.Domain;

public sealed record Room(
    Guid Id,
    Guid HotelId,
    string Name,
    int MaxAdults,
    int MaxChildren);
