using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using ClaimCheckPlayground.Contracts.Models;
using Microsoft.Extensions.Options;

namespace ClaimCheckPlayground.Producer.Services;

/// <summary>
/// Production implementation of <see cref="IClaimCheckService"/>.
/// Uses <see cref="BlobServiceClient"/> and <see cref="ServiceBusSender"/> injected by
/// Aspire's Azure client integrations.
/// </summary>
internal sealed class ClaimCheckService : IClaimCheckService, IAsyncDisposable
{
    private readonly BlobContainerClient _container;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ClaimCheckService> _logger;

    public ClaimCheckService(
        BlobServiceClient blobServiceClient,
        ServiceBusClient serviceBusClient,
        IOptions<ClaimCheckOptions> options,
        ILogger<ClaimCheckService> logger)
    {
        _container = blobServiceClient.GetBlobContainerClient(options.Value.BlobContainerName);
        _sender = serviceBusClient.CreateSender(options.Value.QueueName);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<(string ClaimToken, Guid OrderId)> OffloadAndEnqueueAsync(
        OrderRequest order,
        CancellationToken cancellationToken = default)
    {
        var claimToken = Guid.NewGuid().ToString("N");
        var orderId = Guid.NewGuid();
        var blobName = $"order-payloads/{claimToken}.json";

        // 1. Upload the full payload to Blob Storage
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var json = JsonSerializer.Serialize(order, JsonDefaults.Options);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await _container.UploadBlobAsync(blobName, stream, cancellationToken);

        _logger.LogInformation(
            "Order payload uploaded. BlobName={BlobName} OrderId={OrderId}",
            blobName, orderId);

        // 2. Enqueue a lightweight claim-check message (NO payload on the bus)
        var claim = new ClaimCheckMessage
        {
            ClaimToken = claimToken,
            OrderId = orderId,
            EnqueuedAt = DateTimeOffset.UtcNow
        };

        var sbMessage = new ServiceBusMessage(JsonSerializer.Serialize(claim, JsonDefaults.Options))
        {
            ContentType = "application/json",
            MessageId = orderId.ToString(),
            CorrelationId = claimToken
        };

        await _sender.SendMessageAsync(sbMessage, cancellationToken);

        _logger.LogInformation(
            "Claim-check message enqueued. ClaimToken={ClaimToken} OrderId={OrderId}",
            claimToken, orderId);

        return (claimToken, orderId);
    }

    public async ValueTask DisposeAsync() => await _sender.DisposeAsync();
}
