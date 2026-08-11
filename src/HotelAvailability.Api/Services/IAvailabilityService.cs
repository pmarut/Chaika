using HotelAvailability.Api.Models.Domain;
using HotelAvailability.Api.Models.Requests;

namespace HotelAvailability.Api.Services;

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailableRoom>> SearchAsync(
        SearchAvailabilityRequest request,
        CancellationToken cancellationToken);
}
