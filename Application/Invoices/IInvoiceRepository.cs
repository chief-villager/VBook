using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices;

namespace Bookkeeping.Application.Invoices;

public interface IInvoiceRepository : IGenericRepository<Invoice>
{
    Task<Invoice?> GetAsync(InvoiceId id, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> ListAsync(BusinessId businessId, CancellationToken ct = default);

    // Invoice numbers are unique per business; the service checks this before creating.
    
    /// <summary>
    ///  This is used to check if an invoice for a business already exsit
    /// </summary>
    /// <param name="businessId"></param>
    /// <param name="invoiceNumber"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> NumberExistsAsync(BusinessId businessId, string invoiceNumber, CancellationToken ct = default);
}
