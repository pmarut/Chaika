using HotelAvailability.Api.Models.Requests;

namespace HotelAvailability.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        // Model binding alone (non-nullable fields required, JSON type checks) is the only
        // validation here — this endpoint always returns 501, so a semantic validator (email
        // format etc.) would just be dead code duplicating SearchAvailabilityRequestValidator's role.
        app.MapPost("/api/bookings", (CreateBookingRequest request) => Results.Problem(
                statusCode: StatusCodes.Status501NotImplemented,
                title: "Not Implemented",
                detail: "Booking creation is not implemented yet."))
            .WithName("CreateBooking")
            .WithSummary("Create a booking (stub — not implemented)")
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesValidationProblem();

        return app;
    }
}
