using System.Globalization;
using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Invoices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bookkeeping.Infrastructure.Documents;

// QuestPDF invoice document. Stateless and thread-safe, so it is registered as a
// singleton. The license is set once at startup in Program.cs.
public sealed class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(InvoiceDetail invoice, InvoiceTemplateDto? template, byte[]? logo = null)
        => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Element(header => ComposeHeader(header, invoice, template, logo));
                page.Content().PaddingVertical(20).Element(content => ComposeContent(content, invoice));
                page.Footer().Element(footer => ComposeFooter(footer, template));
            });
        }).GeneratePdf();

    private static void ComposeHeader(IContainer container, InvoiceDetail invoice, InvoiceTemplateDto? template, byte[]? logo)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                // Prefer the logo when we have one; otherwise fall back to the business name.
                if (logo is { Length: > 0 })
                    col.Item().Height(50).AlignLeft().Image(logo).FitHeight();

                col.Item().Text(template?.BusinessName is { Length: > 0 } name ? name : "Invoice")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
            });

            row.ConstantItem(180).Column(col =>
            {
                col.Item().AlignRight().Text("INVOICE").FontSize(16).Bold();
                col.Item().AlignRight().Text($"# {invoice.InvoiceNumber}");
                col.Item().AlignRight().Text($"Issued: {invoice.IssueDate:yyyy-MM-dd}");
                col.Item().AlignRight().Text($"Due: {invoice.DueDate:yyyy-MM-dd}");
                col.Item().AlignRight().Text($"Status: {invoice.Status}");
            });
        });
    }

    private static void ComposeContent(IContainer container, InvoiceDetail invoice)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(15).Column(billTo =>
            {
                billTo.Item().Text("Bill To").SemiBold().FontColor(Colors.Grey.Darken1);
                billTo.Item().Text(invoice.BillTo);
            });

            col.Item().Element(table => ComposeLineItems(table, invoice));
            col.Item().AlignRight().PaddingTop(15).Element(totals => ComposeTotals(totals, invoice));
        });
    }

    private static void ComposeLineItems(IContainer container, InvoiceDetail invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Description");
                header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                header.Cell().Element(HeaderCell).AlignRight().Text("Unit Price");
                header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
            });

            foreach (var line in invoice.LineItems)
            {
                table.Cell().Element(BodyCell).Text(line.Description);
                table.Cell().Element(BodyCell).AlignRight().Text(Number(line.Quantity));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitPrice));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Quantity * line.UnitPrice));
            }
        });

        static IContainer HeaderCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(5).DefaultTextStyle(x => x.SemiBold());

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    }

    private static void ComposeTotals(IContainer container, InvoiceDetail invoice)
    {
        container.Width(220).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal");
                row.ConstantItem(100).AlignRight().Text(Money(invoice.Subtotal));
            });
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"VAT ({invoice.VatRate:P2})");
                row.ConstantItem(100).AlignRight().Text(Money(invoice.VatAmount));
            });
            col.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Darken1).Row(row =>
            {
                row.RelativeItem().Text("Total").Bold();
                row.ConstantItem(100).AlignRight().Text(Money(invoice.TotalAmount)).Bold();
            });
        });
    }

    private static void ComposeFooter(IContainer container, InvoiceTemplateDto? template)
    {
        if (template is null)
            return;

        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(template.BankName) || !string.IsNullOrWhiteSpace(template.AccountNumber))
            {
                col.Item().Text("Payment Details").SemiBold().FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{template.BankName}  •  {template.AccountNumber}".Trim());
            }

            if (!string.IsNullOrWhiteSpace(template.Terms))
            {
                col.Item().PaddingTop(5).Text("Terms").SemiBold().FontColor(Colors.Grey.Darken1);
                col.Item().Text(template.Terms);
            }
        });
    }

    // Two-decimal money with thousands separators, currency-symbol free so the
    // document is not tied to a locale.
    private static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

    private static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
