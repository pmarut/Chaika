using HotelAvailability.Api.Models.Common;
using HotelAvailability.Api.Models.Requests;
using HotelAvailability.Api.Services;

namespace HotelAvailability.Api.Tests.Services;

public class MockAvailabilityServiceTests
{
    private readonly InMemoryHotelCatalog _catalog = new();
    private readonly MockAvailabilityService _sut;

    public MockAvailabilityServiceTests()
    {
        _sut = new MockAvailabilityService(_catalog);
    }

    private static SearchAvailabilityRequest ValidRequest(
        Guid hotelId,
        int rooms = 1,
        int adults = 1,
        IReadOnlyList<int>? childrenAges = null) => new(
            hotelId,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            rooms,
            adults,
            childrenAges);

    [Fact]
    public async Task SearchAsync_WithValidRequest_ReturnsNonEmptyRoomsEachWithAtLeastOneRatePlan()
    {
        var request = ValidRequest(InMemoryHotelCatalog.GrandPlazaId);

        var result = await _sut.SearchAsync(request, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, room => Assert.NotEmpty(room.RatePlans));
    }

    [Fact]
    public async Task SearchAsync_FreeCancellationDeadline_IsBeforeCheckIn()
    {
        var request = ValidRequest(InMemoryHotelCatalog.GrandPlazaId);

        var result = await _sut.SearchAsync(request, CancellationToken.None);

        var checkInStart = request.CheckIn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        foreach (var room in result)
        {
            foreach (var rate in room.RatePlans)
            {
                if (rate.CancellationPolicy is CancellationPolicy.FreeCancellationUntil freeCancellation)
                {
                    Assert.True(freeCancellation.Deadline.UtcDateTime < checkInStart);
                }
            }
        }
    }

    [Fact]
    public async Task SearchAsync_ExcludesRoomsThatDoNotFitRequestedGuests()
    {
        // Grand Plaza's largest room fits at most 2 adults per room (see InMemoryHotelCatalog).
        var request = ValidRequest(InMemoryHotelCatalog.GrandPlazaId, rooms: 1, adults: 5);

        var result = await _sut.SearchAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_ForUnknownHotel_ReturnsEmptyList()
    {
        var request = ValidRequest(Guid.NewGuid());

        var result = await _sut.SearchAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }
}
