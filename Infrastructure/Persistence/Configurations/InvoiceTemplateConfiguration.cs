using Bookkeeping.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookkeeping.Infrastructure.Persistence.Configurations;

public sealed class InvoiceTemplateConfiguration : IEntityTypeConfiguration<InvoiceTemplate>
{
    public void Configure(EntityTypeBuilder<InvoiceTemplate> builder)
    {
        builder.ToTable("invoice_templates", "identity");

        // Shared primary key: the template lives and dies with its business,
        // so BusinessId is both the key here and the FK back to identity.businesses.
        builder.HasKey(t => t.BusinessId);

        builder.Property(t => t.LogoUrl).HasMaxLength(512).IsRequired();
        builder.Property(t => t.BusinessName).HasMaxLength(256).IsRequired();
        builder.Property(t => t.AccountNumber).HasMaxLength(64);
        builder.Property(t => t.BankName).HasMaxLength(128);
        builder.Property(t => t.Terms).HasMaxLength(2000);

        // One-to-one with Business over the shared BusinessId key. Deleting the
        // business removes its template.
        builder.HasOne<Business>()
            .WithOne(b => b.Template)
            .HasForeignKey<InvoiceTemplate>(t => t.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
