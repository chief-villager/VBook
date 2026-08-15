namespace Bookkeeping.Application.Reporting;

// Renders a computed financial statement to a print-ready PDF. The implementation
// lives in Infrastructure so the application layer stays free of the PDF library
// (mirrors IInvoicePdfGenerator). Stateless and thread-safe. The statements are
// passed in already computed — the generator performs no accounting and no I/O.
public interface IFinancialReportPdfGenerator
{
    byte[] ProfitAndLoss(ProfitAndLoss report, string businessName);
    byte[] BalanceSheet(BalanceSheet report, string businessName);
    byte[] CashFlow(CashFlowStatement report, string businessName);
}
