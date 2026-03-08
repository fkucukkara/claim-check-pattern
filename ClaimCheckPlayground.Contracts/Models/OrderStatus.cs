namespace ClaimCheckPlayground.Contracts.Models;

/// <summary>Lifecycle state of an order as it moves through the processing pipeline.</summary>
public enum OrderStatus
{
    /// <summary>The order has been received and a claim-check token issued; processing has not started.</summary>
    Pending,

    /// <summary>The Processor has picked up the claim-check message and is working on the order.</summary>
    Processing,

    /// <summary>The order has been successfully processed and the payload blob deleted.</summary>
    Fulfilled,

    /// <summary>Processing failed; the payload blob may still exist for manual inspection.</summary>
    Failed
}
