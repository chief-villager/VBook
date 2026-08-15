using System.Globalization;
using Bookkeeping.Application.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bookkeeping.Infrastructure.Documents;

// QuestPDF financial-statement documents (profit & loss, balance sheet, cash flow).
// Stateless and thread-safe, so it is registered as a singleton. The Community
// license is set once at startup in Program.cs (shared with the invoice generator).
public sealed class FinancialReportPdfGenerator : IFinancialReportPdfGenerator
{
    public byte[] ProfitAndLoss(ProfitAndLoss report, string businessName)
        => Render(businessName, "Profit & Loss", Period(report.Period.Start, report.Period.End), content =>
            content.Column(col =>
            {
                col.Spacing(18);
                col.Item().Element(c => Section(c, "Revenue", report.Revenue, report.TotalRevenue));
                col.Item().Element(c => Section(c, "Expenses", report.Expenses, report.TotalExpenses));
                col.Item().Element(c => TotalRow(c, "Net profit", report.NetProfit));
            }));

    public byte[] BalanceSheet(BalanceSheet report, string businessName)
        => Render(businessName, "Balance Sheet", $"As of {report.AsOf:yyyy-MM-dd}", content =>
            content.Column(col =>
            {
                col.Spacing(18);
                col.Item().Element(c => Section(c, "Assets", report.Assets, report.TotalAssets));
                col.Item().Element(c => Section(c, "Liabilities", report.Liabilities, report.TotalLiabilities));
                col.Item().Element(c => Section(c, "Equity", report.Equity, report.TotalEquity));
                col.Item().Element(c => TotalRow(c, "Liabilities + Equity", report.TotalLiabilities + report.TotalEquity));
            }));

    public byte[] CashFlow(CashFlowStatement report, string businessName)
        => Render(businessName, "Cash Flow", Period(report.Period.Start, report.Period.End), content =>
            content.Column(col =>
            {
                col.Spacing(2);
                col.Item().Element(c => LineRow(c, "Opening cash", report.OpeningCash));
                col.Item().Element(c => LineRow(c, "Net change in cash", report.NetChange));
                col.Item().Element(c => TotalRow(c, "Closing cash", report.ClosingCash));
            }));

    private static byte[] Render(string businessName, string title, string subtitle, Action<IContainer> content)
        => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Element(header => ComposeHeader(header, businessName, title, subtitle));
                page.Content().PaddingVertical(20).Element(content);
                page.Footer().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span($"Generated {DateTime.UtcNow:yyyy-MM-dd}  •  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

    private static void ComposeHeader(IContainer container, string businessName, string title, string subtitle)
    {
        container.Column(col =>
        {
            col.Item().Text(businessName is { Length: > 0 } ? businessName : "Business")
                .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
            col.Item().PaddingTop(2).Text(title).FontSize(16).Bold();
            col.Item().Text(subtitle).FontColor(Colors.Grey.Darken1);
        });
    }

    // A titled block of statement lines closed by a subtotal row.
    private static void Section(IContainer container, string heading, IReadOnlyList<StatementLineItem> lines, decimal total)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(6).Text(heading).FontSize(13).SemiBold().FontColor(Colors.Blue.Darken2);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                });

                if (lines.Count == 0)
                {
                    table.Cell().Element(BodyCell).Text("No entries in this period.").FontColor(Colors.Grey.Medium);
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(0));
                }

                foreach (var line in lines)
                {
                    table.Cell().Element(BodyCell).Text(line.AccountName);
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Amount));
                }

                table.Cell().Element(SubtotalCell).Text($"Total {heading.ToLowerInvariant()}").SemiBold();
                table.Cell().Element(SubtotalCell).AlignRight().Text(Money(total)).SemiBold();
            });
        });

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        static IContainer SubtotalCell(IContainer c) => c
            .BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(6);
    }

    // A single labelled figure (used by the cash-flow statement, which has no lines).
    private static void LineRow(IContainer container, string label, decimal amount)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(140).AlignRight().Text(Money(amount));
        });
    }

    // The bold closing figure of a statement (net profit, closing cash, …).
    private static void TotalRow(IContainer container, string label, decimal amount)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text(label).Bold().FontSize(12);
            row.ConstantItem(140).AlignRight().Text(Money(amount)).Bold().FontSize(12);
        });
    }

    private static string Period(DateOnly start, DateOnly end) => $"For the period {start:yyyy-MM-dd} to {end:yyyy-MM-dd}";

    // Two-decimal amounts with thousands separators, currency-symbol free so the
    // document is not tied to a locale (matches InvoicePdfGenerator).
    private static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
