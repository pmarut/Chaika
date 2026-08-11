namespace HotelAvailability.Api.Models.Common;

public sealed record Money(decimal Amount, string CurrencyCode); // CurrencyCode: ISO 4217, e.g. "UAH"
