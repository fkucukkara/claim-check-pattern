namespace ClaimCheckPlayground.Contracts.Models;

/// <summary>
/// Represents a customer's order submitted to the Producer API.
/// The full payload is stored in Blob Storage; only the claim token travels through Service Bus.
/// </summary>
public sealed record OrderRequest
{
    /// <summary>Unique identifier of the customer placing the order.</summary>
    public required string CustomerId { get; init; }

    /// <summary>Line items included in this order.</summary>
    public required IReadOnlyList<OrderItem> Items { get; init; }

    /// <summary>Delivery destination for the order.</summary>
    public required ShippingAddress ShippingAddress { get; init; }
}
