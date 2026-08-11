using HotelAvailability.Api.Models.Domain;

namespace HotelAvailability.Api.Services;

public sealed class InMemoryHotelCatalog : IHotelCatalog
{
    // Fixed GUIDs (rather than Guid.NewGuid()) so seed data is deterministic and
    // referenceable by ID from tests and manual requests.
    public static readonly Guid GrandPlazaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeasideResortId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CityCentralId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly IReadOnlyList<Hotel> Hotels =
    [
        new Hotel(GrandPlazaId, "Grand Plaza"),
        new Hotel(SeasideResortId, "Seaside Resort"),
        new Hotel(CityCentralId, "City Central Hotel")
    ];

    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<Room>> RoomsByHotelId =
        new Dictionary<Guid, IReadOnlyList<Room>>
        {
            [GrandPlazaId] =
            [
                new Room(Guid.Parse("11111111-0001-0000-0000-000000000000"), GrandPlazaId, "Deluxe Double Room", MaxAdults: 2, MaxChildren: 1),
                new Room(Guid.Parse("11111111-0002-0000-0000-000000000000"), GrandPlazaId, "Standard Twin Room", MaxAdults: 2, MaxChildren: 0),
                new Room(Guid.Parse("11111111-0003-0000-0000-000000000000"), GrandPlazaId, "Family Suite", MaxAdults: 2, MaxChildren: 3)
            ],
            [SeasideResortId] =
            [
                new Room(Guid.Parse("22222222-0001-0000-0000-000000000000"), SeasideResortId, "Ocean View Room", MaxAdults: 2, MaxChildren: 2),
                new Room(Guid.Parse("22222222-0002-0000-0000-000000000000"), SeasideResortId, "Garden Bungalow", MaxAdults: 4, MaxChildren: 2)
            ],
            [CityCentralId] =
            [
                new Room(Guid.Parse("33333333-0001-0000-0000-000000000000"), CityCentralId, "Economy Single Room", MaxAdults: 1, MaxChildren: 0),
                new Room(Guid.Parse("33333333-0002-0000-0000-000000000000"), CityCentralId, "Business Double Room", MaxAdults: 2, MaxChildren: 1),
                new Room(Guid.Parse("33333333-0003-0000-0000-000000000000"), CityCentralId, "Penthouse Suite", MaxAdults: 4, MaxChildren: 4)
            ]
        };

    public Task<Hotel?> FindAsync(Guid hotelId, CancellationToken cancellationToken)
    {
        var hotel = Hotels.FirstOrDefault(h => h.Id == hotelId);
        return Task.FromResult(hotel);
    }

    public Task<IReadOnlyList<Room>> GetRoomsAsync(Guid hotelId, CancellationToken cancellationToken)
    {
        var rooms = RoomsByHotelId.GetValueOrDefault(hotelId, []);
        return Task.FromResult(rooms);
    }
}
