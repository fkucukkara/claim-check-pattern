using ClaimCheckPlayground.Contracts.Models;
using ClaimCheckPlayground.Producer.Services;

namespace ClaimCheckPlayground.Producer.Endpoints;

/// <summary>
/// Maps the <c>/orders</c> endpoint group for the Producer API.
/// </summary>
internal static class OrderEndpoints
{
    internal static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders")
            .WithTags("Orders");

        group.MapPost("/", SubmitOrderAsync)
            .WithSummary("Submit an order")
            .WithDescription(
                "Applies the Claim-Check pattern: stores the full order payload in Azure Blob Storage " +
                "and enqueues a lightweight claim-check token to Azure Service Bus. " +
                "Returns the claim token that can be used to query processing status from the Processor API.")
            .Produces<SubmitOrderResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> SubmitOrderAsync(
        OrderRequest order,
        IClaimCheckService claimCheckService,
        CancellationToken cancellationToken)
    {
        if (order.Items is null || order.Items.Count == 0)
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(order.Items)] = ["At least one order item is required."]
                });

        var (claimToken, orderId) = await claimCheckService.OffloadAndEnqueueAsync(order, cancellationToken);

        return TypedResults.Accepted(
            (string?)null,
            new SubmitOrderResponse(claimToken, orderId));
    }
}

/// <summary>Response returned after a successful order submission.</summary>
/// <param name="ClaimToken">Use this token to poll the Processor API for order status.</param>
/// <param name="OrderId">Logical order identifier for correlation.</param>
internal sealed record SubmitOrderResponse(string ClaimToken, Guid OrderId);
