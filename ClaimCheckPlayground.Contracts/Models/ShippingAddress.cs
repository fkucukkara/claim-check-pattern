namespace ClaimCheckPlayground.Contracts.Models;

/// <summary>Physical delivery address for an order.</summary>
public sealed record ShippingAddress
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public required string Country { get; init; }
}
