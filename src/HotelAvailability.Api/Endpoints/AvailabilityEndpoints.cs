using HotelAvailability.Api.Models.Requests;
using HotelAvailability.Api.Models.Responses;
using HotelAvailability.Api.Services;
using HotelAvailability.Api.Validation;

namespace HotelAvailability.Api.Endpoints;

public static class AvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/availability/search", SearchAsync)
            .WithName("SearchAvailability")
            .WithSummary("Search room availability for a hotel")
            .Produces<SearchAvailabilityResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> SearchAsync(
        SearchAvailabilityRequest request,
        IHotelCatalog hotelCatalog,
        IAvailabilityService availabilityService,
        SearchAvailabilityRequestValidator validator,
        HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;

        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors.ToProblemDictionary());
        }

        var hotel = await hotelCatalog.FindAsync(request.HotelId, cancellationToken);
        if (hotel is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Hotel not found",
                detail: $"No hotel found with id '{request.HotelId}'.");
        }

        var availableRooms = await availabilityService.SearchAsync(request, cancellationToken);
        var response = SearchAvailabilityResponse.Create(hotel, request.CheckIn, request.CheckOut, availableRooms);

        return Results.Ok(response);
    }
}
