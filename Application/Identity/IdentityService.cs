using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly IIdentityRepository _repository;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;

    public IdentityService(IIdentityRepository repository, IAuthService authService, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _authService = authService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserId>> RegisterUserAsync(string email, string displayName, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<UserId>.Failure("Email is required.");

        var user = new User(email.Trim(), displayName.Trim());
        await _repository.AddUserAsync(user, ct);

        // Stage the credentials on the same unit of work. This validates and hashes
        // the password but does not save; if it fails we never call SaveChangesAsync,
        // so the tracked domain user is discarded with the scope — no orphan.
        var credentials = await _authService.CreateCredentialsAsync(user.Id, user.Email, password, ct);
        if (credentials.IsFailure)
            return Result<UserId>.Failure(credentials.Error);

        // One physical write: domain User + ApplicationUser credentials + any domain events.
        await _unitOfWork.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task<Result<BusinessId>> RegisterBusinessAsync(UserId ownerId, string name, BusinessSector sector, CancellationToken ct = default)
    {
        var owner = await _repository.GetUserAsync(ownerId, ct);
        if (owner is null)
            return Result<BusinessId>.Failure("Owner not found.");

        if (string.IsNullOrWhiteSpace(name))
            return Result<BusinessId>.Failure("Business name is required.");

        var business = Business.Register(ownerId, name.Trim(), sector);
        await _repository.AddAsync(business, ct);

        // Saving dispatches BusinessRegistered: the Ledger seeds the chart of accounts
        // and Transactions seeds default categories, all inside this one transaction.
        await _unitOfWork.SaveChangesAsync(ct);
        return business.Id;
    }

    public async Task<Result<BusinessContext>> GetBusinessAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var business = await _repository.GetBusinessAsync(businessId, ct);
        return business is null
            ? Result<BusinessContext>.Failure("Business not found.")
            : new BusinessContext(business.Id, business.Name, business.Sector);
    }

    public async Task<Result> EnsureOwnershipAsync(UserId userId, BusinessId businessId, CancellationToken ct = default)
    {
        var business = await _repository.GetBusinessAsync(businessId, ct);
        if (business is null)
            return Result.Failure("Business not found.");

        return business.OwnerId.Equals(userId)
            ? Result.Success()
            : Result.Failure("User does not own this business.");
    }

    public async Task<Result> SetInvoiceTemplateAsync(BusinessId businessId, string logoUrl,
    string businessName, string accountNumber, string bankName, string terms,
    CancellationToken ct = default)
    {
        var business = await _repository.GetBusinessAsync(businessId, ct);
        if (business is null)
            return Result.Failure("Business not found.");

        var result = business.SetTemplate(logoUrl, businessName, accountNumber, bankName, terms);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<InvoiceTemplateDto>> GetInvoiceTemplateAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var template = await _repository.GetBusinessInvoiceTemplateAsync(businessId, ct);
        return template is null
            ? Result<InvoiceTemplateDto>.Failure("Invoice template not found.")
            : new InvoiceTemplateDto(template.BusinessId, template.LogoUrl, template.BusinessName,
                template.AccountNumber, template.BankName, template.Terms);
    }
}
