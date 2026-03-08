using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using ClaimCheckPlayground.Contracts.Models;
using Microsoft.Extensions.Options;

namespace ClaimCheckPlayground.Processor.Services;

/// <summary>
/// Long-running background service that implements the consumer side of the Claim-Check pattern.
/// <list type="number">
///   <item>Consumes <see cref="ClaimCheckMessage"/> messages from the configured Service Bus queue.</item>
///   <item>Retrieves the full <see cref="OrderRequest"/> payload from Azure Blob Storage using the claim token.</item>
///   <item>Processes the order (business logic lives in <see cref="ProcessOrderAsync"/>).</item>
///   <item>Completes the Service Bus message and optionally deletes the payload blob.</item>
/// </list>
/// </summary>
internal sealed class OrderProcessingService : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IOrderStateStore _stateStore;
    private readonly ProcessorOptions _options;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        ServiceBusClient serviceBusClient,
        BlobServiceClient blobServiceClient,
        IOrderStateStore stateStore,
        IOptions<ProcessorOptions> options,
        ILogger<OrderProcessingService> logger)
    {
        _serviceBusClient = serviceBusClient;
        _blobServiceClient = blobServiceClient;
        _stateStore = stateStore;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Order Processor starting. Queue={Queue} Container={Container}",
            _options.QueueName, _options.BlobContainerName);

        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false   // we complete manually after successful processing
        };

        await using var processor = _serviceBusClient.CreateProcessor(
            _options.QueueName, processorOptions);

        processor.ProcessMessageAsync += OnMessageAsync;
        processor.ProcessErrorAsync += OnErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);

        // Keep alive until shutdown
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* expected on shutdown */ }

        await processor.StopProcessingAsync();

        _logger.LogInformation("Order Processor stopped.");
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        ClaimCheckMessage? claim;

        try
        {
            claim = JsonSerializer.Deserialize<ClaimCheckMessage>(
                args.Message.Body.ToString(), JsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialise claim-check message. Deadlettering.");
            await args.DeadLetterMessageAsync(args.Message, "Invalid JSON", ex.Message, args.CancellationToken);
            return;
        }

        if (claim is null)
        {
            _logger.LogWarning("Received null claim-check message. Deadlettering.");
            await args.DeadLetterMessageAsync(args.Message, "Null message body", cancellationToken: args.CancellationToken);
            return;
        }

        _logger.LogInformation(
            "Received claim-check. ClaimToken={ClaimToken} OrderId={OrderId}",
            claim.ClaimToken, claim.OrderId);

        _stateStore.SetPending(claim.ClaimToken, claim.OrderId);

        var blobName = $"order-payloads/{claim.ClaimToken}.json";
        var container = _blobServiceClient.GetBlobContainerClient(_options.BlobContainerName);
        var blob = container.GetBlobClient(blobName);

        OrderRequest? order;

        try
        {
            // Retrieve the full payload from Blob Storage using the claim token
            var download = await blob.DownloadContentAsync(args.CancellationToken);
            order = JsonSerializer.Deserialize<OrderRequest>(
                download.Value.Content.ToString(), JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve payload from blob. ClaimToken={ClaimToken}",
                claim.ClaimToken);
            _stateStore.SetFailed(claim.ClaimToken, $"Blob retrieval failed: {ex.Message}");
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            return;
        }

        if (order is null)
        {
            _logger.LogError("Blob payload deserialised to null. ClaimToken={ClaimToken}", claim.ClaimToken);
            _stateStore.SetFailed(claim.ClaimToken, "Payload deserialised to null.");
            await args.DeadLetterMessageAsync(args.Message, "Null payload", cancellationToken: args.CancellationToken);
            return;
        }

        _stateStore.SetProcessing(claim.ClaimToken);

        try
        {
            await ProcessOrderAsync(order, claim, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Order processing failed. ClaimToken={ClaimToken} OrderId={OrderId}",
                claim.ClaimToken, claim.OrderId);
            _stateStore.SetFailed(claim.ClaimToken, ex.Message);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            return;
        }

        // Complete the Service Bus message — removes it from the queue
        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        _stateStore.SetFulfilled(claim.ClaimToken);

        _logger.LogInformation(
            "Order fulfilled. ClaimToken={ClaimToken} OrderId={OrderId}",
            claim.ClaimToken, claim.OrderId);

        // Optionally delete the payload blob after successful processing
        if (_options.DeleteBlobAfterProcessing)
        {
            await blob.DeleteIfExistsAsync(cancellationToken: args.CancellationToken);
            _logger.LogInformation("Payload blob deleted. BlobName={BlobName}", blobName);
        }
    }

    /// <summary>
    /// Core business logic for order fulfilment.
    /// Replace or extend this method with real domain logic (inventory check,
    /// payment authorisation, warehouse dispatch, etc.).
    /// </summary>
    private async Task ProcessOrderAsync(
        OrderRequest order,
        ClaimCheckMessage claim,
        CancellationToken cancellationToken)
    {
        var total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        _logger.LogInformation(
            "Processing order. OrderId={OrderId} Customer={CustomerId} " +
            "Items={ItemCount} Total={Total:C} ShipTo={City},{Country}",
            claim.OrderId,
            order.CustomerId,
            order.Items.Count,
            total,
            order.ShippingAddress.City,
            order.ShippingAddress.Country);

        // Simulate async fulfilment work (e.g. inventory reservation, shipping label generation)
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus error. Source={ErrorSource} EntityPath={EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }
}
