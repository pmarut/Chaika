using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HotelAvailability.Api.Models.Requests;
using HotelAvailability.Api.Models.Responses;
using HotelAvailability.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotelAvailability.Api.Tests.Endpoints;

public class AvailabilityEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static SearchAvailabilityRequest ValidRequest(
        Guid hotelId,
        int adults = 2,
        int rooms = 1) => new(
            hotelId,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            rooms,
            adults,
            ChildrenAges: null);

    [Fact]
    public async Task Search_WithValidRequest_Returns200WithRoomsAndCancellationPolicyDiscriminator()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/availability/search",
            ValidRequest(InMemoryHotelCatalog.GrandPlazaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var ratePlans = document.RootElement
            .GetProperty("rooms")[0]
            .GetProperty("ratePlans");

        Assert.True(ratePlans.GetArrayLength() > 0);
        foreach (var ratePlan in ratePlans.EnumerateArray())
        {
            Assert.True(ratePlan.GetProperty("cancellationPolicy").TryGetProperty("type", out var type));
            Assert.True(type.GetString() is "nonRefundable" or "freeCancellationUntil");
        }
    }

    [Fact]
    public async Task Search_WithInvalidRequest_Returns400()
    {
        var invalidRequest = ValidRequest(InMemoryHotelCatalog.GrandPlazaId, rooms: 0);

        var response = await _client.PostAsJsonAsync("/api/availability/search", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_ForUnknownHotel_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/availability/search",
            ValidRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenNoRoomFitsGuests_Returns200WithEmptyRooms()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/availability/search",
            ValidRequest(InMemoryHotelCatalog.GrandPlazaId, adults: 50));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SearchAvailabilityResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Rooms);
    }

    [Fact]
    public async Task CreateBooking_WithValidBody_Returns501()
    {
        var request = new CreateBookingRequest(
            InMemoryHotelCatalog.GrandPlazaId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            Rooms: 1,
            Adults: 2,
            ChildrenAges: null,
            Guest: new GuestInfo("Jane", "Doe", "jane.doe@example.com", Phone: null));

        var response = await _client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithMissingRequiredField_Returns400()
    {
        var incompletePayload = new
        {
            HotelId = InMemoryHotelCatalog.GrandPlazaId,
            RoomId = Guid.NewGuid()
            // RatePlanId, CheckIn, CheckOut, Rooms, Adults, Guest are intentionally omitted.
        };

        var response = await _client.PostAsJsonAsync("/api/bookings", incompletePayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
