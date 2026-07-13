namespace Bookkeeping.Domain.Invoices;

// A single billed line. Owned by its Invoice: it has no identity or lifecycle of
// its own, so it is created through the aggregate and never queried directly.
public sealed class InvoiceLineItem
{
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private InvoiceLineItem() { }

    public InvoiceLineItem(string description, decimal quantity, decimal unitPrice)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
