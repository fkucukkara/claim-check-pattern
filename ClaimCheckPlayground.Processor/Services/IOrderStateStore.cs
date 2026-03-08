using ClaimCheckPlayground.Contracts.Models;

namespace ClaimCheckPlayground.Processor.Services;

/// <summary>
/// In-memory store that tracks the status of every order the Processor has seen.
/// Writes are performed only by <see cref="OrderProcessingService"/>;
/// reads are served by the status endpoint.
/// </summary>
public interface IOrderStateStore
{
    /// <summary>Records the receipt of a new claim-check message for an order.</summary>
    void SetPending(string claimToken, Guid orderId);

    /// <summary>Transitions an order into the <see cref="OrderStatus.Processing"/> state.</summary>
    void SetProcessing(string claimToken);

    /// <summary>Marks an order as successfully fulfilled.</summary>
    void SetFulfilled(string claimToken);

    /// <summary>Marks an order as failed with the given <paramref name="reason"/>.</summary>
    void SetFailed(string claimToken, string reason);

    /// <summary>
    /// Returns the current status of the order identified by <paramref name="claimToken"/>,
    /// or <c>null</c> if the token is unknown.
    /// </summary>
    OrderStatusResponse? Get(string claimToken);
}
