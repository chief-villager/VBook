using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.CreditReadiness;

public interface ICreditReadinessService
{
    Task<DataSufficiency> CheckDataSufficiencyAsync(BusinessId businessId, DateRange window, CancellationToken ct = default);
    Task<Result<CreditReadinessReport>> GenerateReportAsync(BusinessId businessId, DateRange window, CancellationToken ct = default);
    Task<Result<CreditReadinessDashBoard>> CreditReadinessDashBoard(BusinessId businessId, DateRange window, CancellationToken ct = default);
}
