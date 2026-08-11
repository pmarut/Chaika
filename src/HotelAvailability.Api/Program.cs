using System.Text.Json;
using System.Text.Json.Serialization;
using HotelAvailability.Api.Endpoints;
using HotelAvailability.Api.Infrastructure;
using HotelAvailability.Api.Services;
using HotelAvailability.Api.Validation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    // Record constructor parameters without a C# default (e.g. CreateBookingRequest.Guest)
    // must be present in the JSON body, and non-nullable reference-type members reject an
    // explicit null — so binding alone yields 400 for missing required fields without a
    // hand-written validator for the booking stub. Fields meant to be optional (ChildrenAges,
    // Phone) get an explicit `= null` default in their record declaration to opt out.
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
    options.SerializerOptions.RespectNullableAnnotations = true;
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IHotelCatalog, InMemoryHotelCatalog>();
builder.Services.AddSingleton<IAvailabilityService, MockAvailabilityService>(); // stateless, Scoped adds nothing
builder.Services.AddSingleton<SearchAvailabilityRequestValidator>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.MapAvailabilityEndpoints();
app.MapBookingEndpoints();
app.MapHealthEndpoints();

app.Run();

// Entry point class made visible to WebApplicationFactory<Program> in the test project.
public partial class Program;
