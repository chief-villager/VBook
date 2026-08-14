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

    public async Task<Result<InvoiceId>> CreateAsync(CreateInvoiceCommand command, CancellationToken ct = default)
    {
        if (await _repository.NumberExistsAsync(command.BusinessId, command.InvoiceNumber.Trim(), ct))
            return Result<InvoiceId>.Failure($"Invoice number '{command.InvoiceNumber}' already exists for this business.");

        var lineItems = command.LineItems
            .Select(i => new InvoiceLineItem(i.Description, i.Quantity, i.UnitPrice));

        var result = Invoice.Create(
            command.BusinessId, command.InvoiceNumber, command.IssueDate,
            command.DueDate, command.BillTo, command.VatRate, lineItems);
        if (result.IsFailure)
            return Result<InvoiceId>.Failure(result.Error);

        await _repository.AddAsync(result.Value, ct);
        
        await _unitOfWork.SaveChangesAsync(ct);
        return result.Value.Id;
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

    private static InvoiceDetail ToDetail(Invoice invoice) => new(
        invoice.Id, invoice.BusinessId, invoice.InvoiceNumber, invoice.BillTo,
        invoice.IssueDate, invoice.DueDate, invoice.Status, invoice.VatRate,
        invoice.Subtotal, invoice.VatAmount, invoice.TotalAmount,
        invoice.LineItems.Select(li => new InvoiceLineItemDto(li.Description, li.Quantity, li.UnitPrice)).ToList());
}
