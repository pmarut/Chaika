using System.Text.Json.Serialization;

namespace HotelAvailability.Api.Models.Common;

// [JsonPolymorphic]/[JsonDerivedType] are required here: without them System.Text.Json
// serializes derived records through the declared base type and silently drops
// derived-only members (e.g. Deadline), instead of emitting the { "type": ..., ... } shape.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NonRefundable), "nonRefundable")]
[JsonDerivedType(typeof(FreeCancellationUntil), "freeCancellationUntil")]
public abstract record CancellationPolicy
{
    public sealed record NonRefundable : CancellationPolicy;

    public sealed record FreeCancellationUntil(DateTimeOffset Deadline) : CancellationPolicy;
}
