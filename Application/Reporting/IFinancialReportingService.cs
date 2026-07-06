using Bookkeeping.Domain.Common;

namespace Bookkeeping.Application.Reporting;

public interface IFinancialReportingService
{
    Task<ProfitAndLoss> ProfitAndLossAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);
    Task<BalanceSheet> BalanceSheetAsync(BusinessId businessId, DateOnly asOf, CancellationToken ct = default);
    Task<CashFlowStatement> CashFlowAsync(BusinessId businessId, DateRange period, CancellationToken ct = default);
}
