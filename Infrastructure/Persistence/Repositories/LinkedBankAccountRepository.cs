using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Infrastructure.Persistence.Repositories;

public sealed class LinkedBankAccountRepository
    : GenericRepository<LinkedBankAccount>, ILinkedBankAccountRepository
{
    public LinkedBankAccountRepository(AppDbContext context) : base(context) { }

    public Task<LinkedBankAccount?> GetAsync(LinkedBankAccountId id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<LinkedBankAccount>> ListAsync(
        BusinessId businessId, LinkedBankAccountStatus status, CancellationToken ct = default)
        => await Set
            .Where(a => a.BusinessId == businessId && a.Status == status)
            .OrderBy(a => a.InstitutionName)
            .ToListAsync(ct);

    public Task<LinkedBankAccount?> GetActiveByExternalIdAsync(
        BusinessId businessId, string externalAccountId, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(
            a => a.BusinessId == businessId
                && a.ExternalAccountId == externalAccountId
                && a.Status == LinkedBankAccountStatus.Active,
            ct);
}
