namespace HotelAvailability.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok("Healthy"))
            .WithName("Health")
            .ExcludeFromDescription();

        return app;
    }
}
