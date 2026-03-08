using ClaimCheckPlayground.Contracts.Models;
using ClaimCheckPlayground.Processor.Services;

namespace ClaimCheckPlayground.Processor.Endpoints;

/// <summary>
/// Maps the order status query endpoints for the Processor API.
/// </summary>
internal static class OrderStatusEndpoints
{
    internal static IEndpointRouteBuilder MapOrderStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders")
            .WithTags("Orders");

        group.MapGet("/{claimToken}/status", GetOrderStatusAsync)
            .WithSummary("Get order processing status")
            .WithDescription(
                "Returns the current lifecycle status of an order identified by its claim-check token. " +
                "The token was issued by the Producer API at the time of order submission.")
            .Produces<OrderStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult GetOrderStatusAsync(
        string claimToken,
        IOrderStateStore stateStore)
    {
        var status = stateStore.Get(claimToken);
        return status is not null
            ? TypedResults.Ok(status)
            : TypedResults.NotFound();
    }
}
