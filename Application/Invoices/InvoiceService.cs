using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Invoices;

namespace Bookkeeping.Application.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceService(IInvoiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Server-assigned invoice numbers: "INV-00001", sequential per business.
    private const string NumberPrefix = "INV-";
    private static string FormatNumber(int sequence) => $"{NumberPrefix}{sequence:D5}";

    public async Task<Result<InvoiceId>> CreateAsync(CreateInvoiceCommand command, CancellationToken ct = default)
    {
        var invoiceNumber = await NextNumberAsync(command.BusinessId, ct);

        var lineItems = command.LineItems
            .Select(i => new InvoiceLineItem(i.Description, i.Quantity, i.UnitPrice));
        DateOnly issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = Invoice.Create(
            command.BusinessId, invoiceNumber, issueDate,
            command.DueDate, command.BillTo, command.CustomerEmail, command.Note, command.VatRate, lineItems);
        if (result.IsFailure)
            return Result<InvoiceId>.Failure(result.Error);

        await _repository.AddAsync(result.Value, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return result.Value.Id;
    }

    // Derives the next number from the current per-business count. Invoices are never
    // deleted, so count + 1 is the next sequence; the loop is a defensive guard against
    // a rare concurrent create landing on the same number (the unique index is the
    // ultimate backstop).
    private async Task<string> NextNumberAsync(BusinessId businessId, CancellationToken ct)
    {
        var sequence = await _repository.CountForBusinessAsync(businessId, ct) + 1;
        var number = FormatNumber(sequence);
        while (await _repository.NumberExistsAsync(businessId, number, ct))
            number = FormatNumber(++sequence);
        return number;
    }

    public async Task<Result> MarkAsPaidAsync(BusinessId businessId, InvoiceId id, CancellationToken ct = default)
    {
        var found = ResourceOwnership.RequireOwned(await _repository.GetAsync(id, ct), businessId, "Invoice");
        if (found.IsFailure)
            return Result.Failure(found.Error);
        var invoice = found.Value;

        var result = invoice.MarkAsPaid();
        if (result.IsFailure)
            return result;

        _repository.Update(invoice);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<InvoiceDetail?> GetAsync(BusinessId businessId, InvoiceId id, CancellationToken ct = default)
    {
        var found = ResourceOwnership.RequireOwned(await _repository.GetAsync(id, ct), businessId, "Invoice");
        return found.IsSuccess ? ToDetail(found.Value) : null;
    }

    public async Task<PagedResult<InvoiceSummary>> ListAsync(BusinessId businessId, PageRequest page, CancellationToken ct = default)
    {
        var invoices = await _repository.ListAsync(businessId, page, ct);
        var items = invoices.Items
            .Select(i => new InvoiceSummary(
                i.Id, i.InvoiceNumber, i.BillTo, i.IssueDate, i.DueDate, i.Status, i.TotalAmount))
            .ToList();

        return new PagedResult<InvoiceSummary>(items, invoices.Page, invoices.PageSize, invoices.TotalCount);
    }

    public async Task<IReadOnlyCollection<Invoice>> ListAsync(BusinessId businessId, CancellationToken ct = default)
    {
        return await _repository.ListAsync(businessId, ct);
    }

    private static InvoiceDetail ToDetail(Invoice invoice) => new(
        invoice.Id, invoice.BusinessId, invoice.InvoiceNumber, invoice.BillTo, invoice.CustomerEmail,
        invoice.IssueDate, invoice.DueDate, invoice.Status, invoice.VatRate,
        invoice.Subtotal, invoice.VatAmount, invoice.TotalAmount,
        invoice.LineItems.Select(li => new InvoiceLineItemDto(li.Description, li.Quantity, li.UnitPrice)).ToList());
}
