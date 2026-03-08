namespace ClaimCheckPlayground.Contracts.Models;

/// <summary>A single line item within an order.</summary>
public sealed record OrderItem
{
    /// <summary>Catalogue identifier of the product being ordered.</summary>
    public required string ProductId { get; init; }

    /// <summary>Number of units requested.</summary>
    public required int Quantity { get; init; }

    /// <summary>Price per unit at the time of order placement (in the account's billing currency).</summary>
    public required decimal UnitPrice { get; init; }
}
