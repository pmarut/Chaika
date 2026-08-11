using HotelAvailability.Api.Models.Domain;

namespace HotelAvailability.Api.Models.Responses;

public sealed record SearchAvailabilityResponse(
    Guid HotelId,
    string HotelName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    IReadOnlyList<AvailableRoomDto> Rooms)
{
    public static SearchAvailabilityResponse Create(
        Hotel hotel,
        DateOnly checkIn,
        DateOnly checkOut,
        IReadOnlyList<AvailableRoom> availableRooms) => new(
            hotel.Id,
            hotel.Name,
            checkIn,
            checkOut,
            availableRooms.Select(AvailableRoomDto.FromDomain).ToArray());
}
