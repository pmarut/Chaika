using System.Text.Json;
using System.Text.Json.Serialization;
using HotelAvailability.Api.Endpoints;
using HotelAvailability.Api.Infrastructure;
using HotelAvailability.Api.Services;
using HotelAvailability.Api.Validation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

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
