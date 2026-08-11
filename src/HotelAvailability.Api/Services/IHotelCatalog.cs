using HotelAvailability.Api.Models.Domain;

namespace HotelAvailability.Api.Services;

public interface IHotelCatalog
{
    Task<Hotel?> FindAsync(Guid hotelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Room>> GetRoomsAsync(Guid hotelId, CancellationToken cancellationToken);
}
