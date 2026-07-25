using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Abstractions
{
    public interface IBankFeedProvider
    {
            
        /// <summary>Exchanges the short-lived widget code for a durable account handle.</summary>
        Task<BankAccountLink> LinkAccountAsync(string authorisationCode, CancellationToken ct);

        Task<IReadOnlyList<BankFeedTransaction>> GetTransactionsAsync(
            string externalAccountId,
            DateOnly from,
            DateOnly to,
            CancellationToken ct);

        Task UnlinkAccountAsync(string externalAccountId, CancellationToken ct); 
    }

    public sealed record BankAccountLink(
    string ExternalAccountId,
    string InstitutionName,
    string AccountNumberMasked,
    string Currency);

    public sealed record BankFeedTransaction(
        string ExternalId,
        DateTimeOffset OccurredAt,
        decimal Amount,
        BankFeedDirection Direction,
        string Narration,
        string? ProviderCategory); // null unless you opt into Mono's enrichment

    public enum BankFeedDirection { Credit, Debit }
}