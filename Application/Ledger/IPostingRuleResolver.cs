using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Ledger;
using Bookkeeping.Domain.Transactions.Events;

namespace Bookkeeping.Application.Ledger;

// The one place accounting expertise lives: which account to debit and which to
// credit for a given transaction. A richer category-to-account mapping is the
// documented extension point here.
public interface IPostingRuleResolver
{
    Task<Result<IReadOnlyList<PostingLine>>> ResolveAsync(TransactionRecorded evt, CancellationToken ct = default);
}
