using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;
using Bookkeeping.Domain.Transactions;
using Bookkeeping.Domain.Transactions.Events;

namespace Bookkeeping.Application.Ledger;
//this class handles the enforcing and categorizing of transactions into a double entry
//for the journalEntry class to use. TL:DR it just creates the double entry object
public sealed class PostingRuleResolver : IPostingRuleResolver
{
    private readonly ILedgerRepository _ledger;

    public PostingRuleResolver(ILedgerRepository ledger) => _ledger = ledger;

    public async Task<Result<IReadOnlyList<PostingLine>>> ResolveAsync(TransactionRecorded evt, CancellationToken ct = default)
    {
        var cash = await _ledger.GetAccountByCodeAsync(ChartOfAccounts.Cash, ct);
        if (cash is null)
            return Result<IReadOnlyList<PostingLine>>.Failure("Cash account missing. Seed the chart of accounts first.");

        // Each transaction type has one fixed counter-account. Cash is always one
        // side; the other side is the credit (inflows) or debit (expense) below.
        switch (evt.Type)
        {
            case TransactionType.Income:
                return await BuildAsync(ChartOfAccounts.SalesRevenue, "Sales revenue account missing.",
                    revenue => new List<PostingLine>
                    {
                        new(cash.Id,    evt.Amount, 0m),   // debit cash: an asset went up
                        new(revenue.Id, 0m, evt.Amount),   // credit revenue: income went up
                    }, ct);

            case TransactionType.Capital:
                return await BuildAsync(ChartOfAccounts.OwnersCapital, "Owner's capital account missing.",
                    capital => new List<PostingLine>
                    {
                        new(cash.Id,    evt.Amount, 0m),   // debit cash: an asset went up
                        new(capital.Id, 0m, evt.Amount),   // credit equity: the owner put money in
                    }, ct);

            case TransactionType.Loan:
                return await BuildAsync(ChartOfAccounts.LoansPayable, "Loans payable account missing.",
                    loan => new List<PostingLine>
                    {
                        new(cash.Id, evt.Amount, 0m),      // debit cash: an asset went up
                        new(loan.Id, 0m, evt.Amount),      // credit liability: the business owes the money
                    }, ct);

            case TransactionType.Expense:
                return await BuildAsync(ChartOfAccounts.GeneralExpenses, "Expenses account missing.",
                    expense => new List<PostingLine>
                    {
                        new(expense.Id, evt.Amount, 0m),   // debit expense
                        new(cash.Id,    0m, evt.Amount),   // credit cash: an asset went down
                    }, ct);

            default:
                return Result<IReadOnlyList<PostingLine>>.Failure($"No posting rule for transaction type {evt.Type}.");
        }
    }

    // Loads the counter-account by code, then hands it to the caller's builder.
    // Keeps the account-missing check in one place for every rule.
    private async Task<Result<IReadOnlyList<PostingLine>>> BuildAsync(
        string counterAccountCode, string missingMessage,
        Func<Account, List<PostingLine>> build, CancellationToken ct)
    {
        var counter = await _ledger.GetAccountByCodeAsync(counterAccountCode, ct);
        if (counter is null)
            return Result<IReadOnlyList<PostingLine>>.Failure(missingMessage);

        return Result<IReadOnlyList<PostingLine>>.Success(build(counter));
    }
}
