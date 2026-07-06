using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;

namespace Bookkeeping.Application.Ledger;

public sealed record AccountBalance(
    AccountId Id,
    string Code,
    string Name,
    AccountType Type,
    string StatementLine,
    decimal Balance);   // signed: debits minus credits

public sealed record TrialBalance(
    BusinessId Business,
    DateOnly AsOf,
    IReadOnlyList<AccountBalance> Accounts,
    decimal TotalDebits,
    decimal TotalCredits)
{
    public bool IsBalanced => TotalDebits == TotalCredits;
}
