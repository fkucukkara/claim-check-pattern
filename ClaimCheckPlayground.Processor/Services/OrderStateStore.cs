using System.Collections.Concurrent;
using ClaimCheckPlayground.Contracts.Models;

namespace ClaimCheckPlayground.Processor.Services;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IOrderStateStore"/>.
/// Suitable for demo purposes; replace with a durable store (e.g. Azure Table Storage,
/// Cosmos DB) for production workloads.
/// </summary>
internal sealed class OrderStateStore : IOrderStateStore
{
    private readonly ConcurrentDictionary<string, OrderStatusResponse> _store = new();

    public void SetPending(string claimToken, Guid orderId) =>
        _store[claimToken] = new OrderStatusResponse
        {
            ClaimToken = claimToken,
            OrderId = orderId,
            Status = OrderStatus.Pending
        };

    public void SetProcessing(string claimToken) =>
        _store.AddOrUpdate(
            claimToken,
            _ => new OrderStatusResponse { ClaimToken = claimToken, OrderId = Guid.Empty, Status = OrderStatus.Processing },
            (_, existing) => existing with { Status = OrderStatus.Processing });

    public void SetFulfilled(string claimToken) =>
        _store.AddOrUpdate(
            claimToken,
            _ => new OrderStatusResponse { ClaimToken = claimToken, OrderId = Guid.Empty, Status = OrderStatus.Fulfilled, ProcessedAt = DateTimeOffset.UtcNow },
            (_, existing) => existing with { Status = OrderStatus.Fulfilled, ProcessedAt = DateTimeOffset.UtcNow });

    public void SetFailed(string claimToken, string reason) =>
        _store.AddOrUpdate(
            claimToken,
            _ => new OrderStatusResponse { ClaimToken = claimToken, OrderId = Guid.Empty, Status = OrderStatus.Failed, ProcessedAt = DateTimeOffset.UtcNow, FailureReason = reason },
            (_, existing) => existing with { Status = OrderStatus.Failed, ProcessedAt = DateTimeOffset.UtcNow, FailureReason = reason });

    public OrderStatusResponse? Get(string claimToken) =>
        _store.TryGetValue(claimToken, out var response) ? response : null;
}
