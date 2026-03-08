namespace ClaimCheckPlayground.Contracts.Models;

/// <summary>
/// Lightweight message sent to Azure Service Bus.
/// Contains only the claim-check token that the Processor uses to retrieve
/// the full order payload from Blob Storage — the payload itself never travels through the bus.
/// </summary>
public sealed record ClaimCheckMessage
{
    /// <summary>
    /// Unique token identifying the blob that holds the full order payload.
    /// Used as the blob name: <c>order-payloads/{ClaimToken}.json</c>.
    /// </summary>
    public required string ClaimToken { get; init; }

    /// <summary>Logical identifier of the order, echoed back in status responses.</summary>
    public required Guid OrderId { get; init; }

    /// <summary>UTC timestamp when the message was enqueued by the Producer.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }
}
