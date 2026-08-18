using Bookkeeping.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", "invoices");
        builder.HasKey(i => i.Id);
        builder.Ignore(i => i.DomainEvents);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(64).IsRequired();
        builder.Property(i => i.BillTo).HasMaxLength(256).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Note).HasMaxLength(2000);

        // Populated out of band once the PDF is rendered and stored; null until then.
        builder.Property(i => i.PdfUrl).HasMaxLength(1024);

        // Override the global decimal(18,2) convention: a VAT rate like 0.075 needs the extra scale.
        builder.Property(i => i.VatRate).HasPrecision(5, 4);

        // Subtotal / VatAmount / TotalAmount are derived from the lines, never stored.
        builder.Ignore(i => i.Subtotal);
        builder.Ignore(i => i.VatAmount);
        builder.Ignore(i => i.TotalAmount);

        // Invoice numbers are unique within a business. BusinessId crosses into Identity: value only, no FK.
        builder.HasIndex(i => new { i.BusinessId, i.InvoiceNumber }).IsUnique();

        // Line items live and die with the invoice: an owned collection in its own table.
        builder.OwnsMany(i => i.LineItems, line =>
        {
            line.ToTable("invoice_line_items", "invoices");
            line.WithOwner().HasForeignKey("InvoiceId");
            line.Property<int>("Id");
            line.HasKey("Id");

            line.Property(l => l.Description).HasMaxLength(256).IsRequired();
            line.Property(l => l.Quantity).HasPrecision(18, 2);
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
            line.Ignore(l => l.LineTotal);
        });

        // Read-only LineItems property is backed by the _lineItems field.
        builder.Navigation(i => i.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class InvoicePdfJobConfiguration : IEntityTypeConfiguration<InvoicePdfJob>
{
    public void Configure(EntityTypeBuilder<InvoicePdfJob> builder)
    {
        builder.ToTable("pdf_jobs", "invoices");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.Error).HasMaxLength(2000);

        // The processor polls for pending rows oldest-first, so index the queue key.
        builder.HasIndex(j => j.Status);
    }
}
