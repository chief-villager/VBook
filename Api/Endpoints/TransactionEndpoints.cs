using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Api.Endpoints;

public static class TransactionEndpoints
{
    public sealed record RecordTransactionRequest(decimal Amount, Guid CategoryId, DateOnly OccurredOn, string? Note);

    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/businesses/{businessId:guid}/categories",
            async (Guid businessId, ITransactionService transactions, CancellationToken ct) =>
                Results.Ok(await transactions.GetCategoriesAsync(new BusinessId(businessId), ct)));

        app.MapPost("/api/businesses/{businessId:guid}/transactions",
            async (Guid businessId, RecordTransactionRequest body, ITransactionService transactions, CancellationToken ct) =>
            {
                var command = new RecordTransactionCommand(
                    new BusinessId(businessId), body.Amount, new CategoryId(body.CategoryId), body.OccurredOn, body.Note);
                var result = await transactions.RecordAsync(command, ct);
                return result.IsSuccess
                    ? Results.Ok(new { transactionId = result.Value.Value })
                    : Results.BadRequest(new { error = result.Error });
            });

        app.MapGet("/api/businesses/{businessId:guid}/transactions",
            async (Guid businessId, DateOnly from, DateOnly to, ITransactionService transactions, CancellationToken ct) =>
                Results.Ok(await transactions.ListAsync(new BusinessId(businessId), new DateRange(from, to), ct)));

        return app;
    }
}
