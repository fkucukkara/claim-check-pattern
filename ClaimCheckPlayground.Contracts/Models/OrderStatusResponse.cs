namespace ClaimCheckPlayground.Contracts.Models;

/// <summary>
/// Response DTO returned by the Processor's status endpoint.
/// </summary>
public sealed record OrderStatusResponse
{
    /// <summary>The claim-check token originally issued by the Producer.</summary>
    public required string ClaimToken { get; init; }

    /// <summary>Logical order identifier.</summary>
    public required Guid OrderId { get; init; }

    /// <summary>Current lifecycle status of the order.</summary>
    public required OrderStatus Status { get; init; }

    /// <summary>UTC timestamp when the Processor completed handling; <c>null</c> while still pending/processing.</summary>
    public DateTimeOffset? ProcessedAt { get; init; }

    /// <summary>Human-readable details about a processing failure, if applicable.</summary>
    public string? FailureReason { get; init; }
}
