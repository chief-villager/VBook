using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.Common;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;

namespace Bookkeeping.Application.Identity;

public sealed class IdentityService : IIdentityService
{
    // Supported logo image types mapped to the file extension used in the object key.
    private static readonly IReadOnlyDictionary<string, string> LogoExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp",
            ["image/svg+xml"] = ".svg",
        };

    private readonly IIdentityRepository _repository;
    private readonly IAuthService _authService;
    private readonly IObjectStore _objectStore;
    private readonly IUnitOfWork _unitOfWork;

    public IdentityService(IIdentityRepository repository, IAuthService authService,
        IObjectStore objectStore, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _authService = authService;
        _objectStore = objectStore;
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

        // Send the confirmation link now the account is committed (best-effort — a
        // delivery failure doesn't undo the registration).
        await _authService.SendEmailConfirmationAsync(user.Email, ct);
        return user.Id;
    }

    public async Task<Result<BusinessRegistrationResult>> RegisterBusinessWithOwnerAsync(
        string email, string displayName, string password,
        string businessName, BusinessSector sector, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<BusinessRegistrationResult>.Failure("Email is required.");
        if (string.IsNullOrWhiteSpace(businessName))
            return Result<BusinessRegistrationResult>.Failure("Business name is required.");

        var existing = await _repository.GetUserByEmailAsync(email.Trim(), ct);
        if (existing is not null)
            return Result<BusinessRegistrationResult>.Failure("A user with this email already exists.");

        // Everything below is staged on one unit of work and committed by a single
        // SaveChangesAsync — the owner, their credentials, the business, and the owner
        // membership either all persist or none do.
        var user = new User(email.Trim(), displayName.Trim());
        await _repository.AddUserAsync(user, ct);

        var credentials = await _authService.CreateCredentialsAsync(user.Id, user.Email, password, ct);
        if (credentials.IsFailure)
            return Result<BusinessRegistrationResult>.Failure(credentials.Error);

        var business = Business.Register(user.Id, businessName.Trim(), sector);
        await _repository.AddAsync(business, ct);

        var membership = BusinessMembership.Create(business.Id, user.Id, BusinessRole.Owner);
        await _repository.AddMembershipAsync(membership, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        // Send the confirmation link now the account is committed (best-effort).
        await _authService.SendEmailConfirmationAsync(user.Email, ct);
        return new BusinessRegistrationResult(user.Id, business.Id);
    }

    public async Task<Result<UserId>> AddMemberAsync(
        BusinessId businessId, string email, string displayName, string password,
        BusinessRole role, CancellationToken ct = default)
    {
        if (role == BusinessRole.Owner)
            return Result<UserId>.Failure("A business already has an owner; assign a different role.");
        if (string.IsNullOrWhiteSpace(email))
            return Result<UserId>.Failure("Email is required.");

        var business = await _repository.GetBusinessAsync(businessId, ct);
        if (business is null)
            return Result<UserId>.Failure("Business not found.");

        // This flow provisions a brand-new user account for the member. A person who
        // already has an account is rejected here rather than silently reused.
        var existing = await _repository.GetUserByEmailAsync(email.Trim(), ct);
        if (existing is not null)
            return Result<UserId>.Failure("A user with this email already exists.");

        var user = new User(email.Trim(), displayName.Trim());
        await _repository.AddUserAsync(user, ct);

        var credentials = await _authService.CreateCredentialsAsync(user.Id, user.Email, password, ct);
        if (credentials.IsFailure)
            return Result<UserId>.Failure(credentials.Error);

        var membership = BusinessMembership.Create(businessId, user.Id, role);
        await _repository.AddMembershipAsync(membership, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        // Send the confirmation link so the new member can activate and sign in (best-effort).
        await _authService.SendEmailConfirmationAsync(user.Email, ct);
        return user.Id;
    }

    public async Task<Result<IReadOnlyList<BusinessMemberDto>>> ListMembersAsync(BusinessId businessId, CancellationToken ct = default)
    {
        var business = await _repository.GetBusinessAsync(businessId, ct);
        if (business is null)
            return Result<IReadOnlyList<BusinessMemberDto>>.Failure("Business not found.");

        var memberships = await _repository.ListMembershipsAsync(businessId, ct);

        var members = new List<BusinessMemberDto>(memberships.Count);
        foreach (var m in memberships)
        {
            var user = await _repository.GetUserAsync(m.UserId, ct);
            members.Add(new BusinessMemberDto(
                m.UserId, user?.Email ?? string.Empty, user?.DisplayName ?? string.Empty, m.Role, m.JoinedAt));
        }

        return members;
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(UserId userId, CancellationToken ct = default)
    {
        var user = await _repository.GetUserAsync(userId, ct);
        if (user is null)
            return Result<CurrentUserDto>.Failure("User not found.");

        var memberships = await _repository.ListMembershipsForUserAsync(userId, ct);

        var list = new List<UserMembershipDto>(memberships.Count);
        foreach (var m in memberships)
        {
            var business = await _repository.GetBusinessAsync(m.BusinessId, ct);
            list.Add(new UserMembershipDto(m.BusinessId, business?.Name ?? string.Empty, m.Role, m.JoinedAt));
        }

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, list);
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

    public async Task<Result> SetInvoiceTemplateAsync(BusinessId businessId,
    Stream logo, string logoContentType,
    string businessName, string accountNumber, string bankName, string terms,
    CancellationToken ct = default)
    {
        if (!LogoExtensions.TryGetValue(logoContentType ?? string.Empty, out var extension))
            return Result.Failure("Unsupported logo type. Allowed: PNG, JPEG, WEBP, SVG.");

        var business = await _repository.GetBusinessAsync(businessId, ct);
        if (business is null)
            return Result.Failure("Business not found.");

        // Derive the key server-side rather than trusting a client-supplied filename.
        var key = $"logos/{businessId.Value}/{Guid.NewGuid():N}{extension}";
        // Non-null: it matched a LogoExtensions key above.
        var logoUrl = await _objectStore.PutAsync(key, logo, logoContentType!, ct);

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
