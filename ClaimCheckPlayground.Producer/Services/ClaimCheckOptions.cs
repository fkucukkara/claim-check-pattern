namespace ClaimCheckPlayground.Producer.Services;

/// <summary>Configuration options for the Claim-Check service.</summary>
public sealed class ClaimCheckOptions
{
    /// <summary>
    /// Name of the Azure Blob Storage container where order payloads are stored.
    /// Defaults to <c>order-payloads</c>.
    /// </summary>
    public string BlobContainerName { get; set; } = "order-payloads";

    /// <summary>
    /// Name of the Azure Service Bus queue where claim-check messages are sent.
    /// Defaults to <c>orders</c>.
    /// </summary>
    public string QueueName { get; set; } = "orders";
}
