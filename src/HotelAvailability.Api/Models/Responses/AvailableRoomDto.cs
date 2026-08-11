using HotelAvailability.Api.Models.Domain;

namespace HotelAvailability.Api.Models.Responses;

public sealed record AvailableRoomDto(
    Guid RoomId,
    string Name,
    IReadOnlyList<RatePlanDto> RatePlans)
{
    public static AvailableRoomDto FromDomain(AvailableRoom availableRoom) => new(
        availableRoom.Room.Id,
        availableRoom.Room.Name,
        availableRoom.RatePlans.Select(RatePlanDto.FromDomain).ToArray());
}
