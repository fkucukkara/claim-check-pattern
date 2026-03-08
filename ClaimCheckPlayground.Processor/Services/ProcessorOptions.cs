namespace ClaimCheckPlayground.Processor.Services;

/// <summary>Configuration options for the Processor background service.</summary>
public sealed class ProcessorOptions
{
    /// <summary>
    /// Name of the Azure Blob Storage container that holds order payloads.
    /// Must match the value configured in the Producer.
    /// </summary>
    public string BlobContainerName { get; set; } = "order-payloads";

    /// <summary>Name of the Azure Service Bus queue to consume.</summary>
    public string QueueName { get; set; } = "orders";

    /// <summary>
    /// When <c>true</c> (default), the payload blob is deleted from Blob Storage
    /// after it has been successfully processed.
    /// Set to <c>false</c> during debugging to inspect payloads.
    /// </summary>
    public bool DeleteBlobAfterProcessing { get; set; } = true;
}
