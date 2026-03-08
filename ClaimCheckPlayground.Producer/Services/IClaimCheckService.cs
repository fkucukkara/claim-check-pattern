using ClaimCheckPlayground.Contracts.Models;

namespace ClaimCheckPlayground.Producer.Services;

/// <summary>
/// Encapsulates the Claim-Check pattern for outbound orders:
/// <list type="number">
///   <item>Generates a unique claim token.</item>
///   <item>Serialises the full order payload and uploads it to Blob Storage.</item>
///   <item>Enqueues a lightweight <see cref="ClaimCheckMessage"/> to Service Bus.</item>
/// </list>
/// The messaging bus therefore never carries the heavy payload.
/// </summary>
public interface IClaimCheckService
{
    /// <summary>
    /// Stores <paramref name="order"/> in Blob Storage and enqueues a claim-check token to Service Bus.
    /// </summary>
    /// <param name="order">The full order payload to offload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple of the generated <c>claimToken</c> (blob key) and the
    /// logical <c>orderId</c> (correlation ID in Service Bus message).
    /// </returns>
    Task<(string ClaimToken, Guid OrderId)> OffloadAndEnqueueAsync(
        OrderRequest order,
        CancellationToken cancellationToken = default);
}
