using Bookkeeping.Application.Abstractions;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Infrastructure.Mono;

// Adapter: implements the provider-neutral IBankFeedProvider the application depends on
// by delegating to MonoApiClient and mapping Mono's payloads onto our own records.
// Keeping the translation here means the rest of the app never sees a Mono type.
internal sealed class MonoBankFeedProvider(MonoApiClient client) : IBankFeedProvider
{
    public async Task<BankAccountLink> LinkAccountAsync(string authorisationCode, CancellationToken ct)
    {
        // Trade the widget's short-lived code for a durable account id, then read the account.
        var exchange = await client.ExchangeTokenAsync(authorisationCode, ct);
        var response = await client.GetAccountAsync(exchange.Id, ct);

        var account = response.Data?.Account
            ?? throw new MonoApiException(System.Net.HttpStatusCode.OK, "Account payload was empty.");

        return new BankAccountLink(
            ExternalAccountId: exchange.Id,
            InstitutionName: account.Institution?.Name ?? "Unknown",
            AccountNumberMasked: MaskAccountNumber(account.AccountNumber),
            Currency: account.Currency ?? "NGN");
    }

    public async Task<IReadOnlyList<BankFeedTransaction>> GetTransactionsAsync(
        string externalAccountId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var results = new List<BankFeedTransaction>();

        // Mono paginates; follow Meta.Next until it runs dry.
        string? cursor = null;
        do
        {
            var page = await client.GetTransactionsAsync(externalAccountId, from, to, cursor, ct);
            foreach (var tx in page.Data)
                results.Add(Map(tx));

            cursor = page.Meta.Next;
        }
        while (cursor is not null);

        return results;
    }

    public Task UnlinkAccountAsync(string externalAccountId, CancellationToken ct)
        => client.UnlinkAsync(externalAccountId, ct);

    private static BankFeedTransaction Map(MonoTransaction tx) => new(
        ExternalId: tx.Id ?? string.Empty,
        OccurredAt: tx.Date,
        // Mono reports amounts in minor units (kobo); our Money is in major units.
        Amount: new decimal((double)(tx.Amount / 100m)),
        Direction: string.Equals(tx.Type, "credit", StringComparison.OrdinalIgnoreCase)
            ? BankFeedDirection.Credit
            : BankFeedDirection.Debit,
        Narration: tx.Narration ?? string.Empty,
        ProviderCategory: tx.Category);

    // Show only the last four digits; everything else becomes bullets.
    private static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber))
            return string.Empty;

        const int visible = 4;
        if (accountNumber.Length <= visible)
            return accountNumber;

        return new string('*', accountNumber.Length - visible) + accountNumber[^visible..];
    }
}
