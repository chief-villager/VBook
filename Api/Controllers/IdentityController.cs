using Bookkeeping.Application.Identity;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

[ApiController]
public sealed class IdentityController(IIdentityService identity, IAuthService auth) : ControllerBase
{
    public sealed record RegisterUserRequest(string Email, string DisplayName, string Password);
    public sealed record RegisterBusinessRequest(Guid OwnerId, string Name, BusinessSector Sector);
    public sealed record RegisterBusinessWithOwnerRequest(
        string Email, string DisplayName, string Password, string BusinessName, BusinessSector Sector);
    public sealed record AddMemberRequest(string Email, string DisplayName, string Password, BusinessRole Role);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record SetInvoiceTemplateRequest(string LogoUrl, string BusinessName,
        string AccountNumber, string BankName, string Terms);

    [HttpPost("api/users")]
    public async Task<IActionResult> RegisterUser(RegisterUserRequest body, CancellationToken ct)
    {
        var result = await identity.RegisterUserAsync(body.Email, body.DisplayName, body.Password, ct);
        return result.IsSuccess
            ? Ok(new { userId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("api/auth/login")]
    public async Task<IActionResult> Login(LoginRequest body, CancellationToken ct)
    {
        var result = await auth.SignInAsync(body.Email, body.Password, ct);
        return result.IsSuccess
            ? Ok(new { token = result.Value })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("api/businesses")]
    public async Task<IActionResult> RegisterBusiness(RegisterBusinessRequest body, CancellationToken ct)
    {
        var result = await identity.RegisterBusinessAsync(new UserId(body.OwnerId), body.Name, body.Sector, ct);
        return result.IsSuccess
            ? Ok(new { businessId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    // Combined signup: creates the owner and their business in one transaction.
    [HttpPost("api/businesses/register")]
    public async Task<IActionResult> RegisterBusinessWithOwner(RegisterBusinessWithOwnerRequest body, CancellationToken ct)
    {
        var result = await identity.RegisterBusinessWithOwnerAsync(
            body.Email, body.DisplayName, body.Password, body.BusinessName, body.Sector, ct);
        return result.IsSuccess
            ? Ok(new { ownerId = result.Value.OwnerId.Value, businessId = result.Value.BusinessId.Value })
            : BadRequest(new { error = result.Error });
    }

    // Adds another user to a business with a non-owner role. Only a caller whose role
    // for this business grants Users.Create (the Owner) is allowed.
    [Authorize(Policy = Permissions.Users.Create)]
    [HttpPost("api/businesses/{businessId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid businessId, AddMemberRequest body, CancellationToken ct)
    {
        var result = await identity.AddMemberAsync(
            new BusinessId(businessId), body.Email, body.DisplayName, body.Password, body.Role, ct);
        return result.IsSuccess
            ? Ok(new { userId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("api/businesses/{businessId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid businessId, CancellationToken ct)
    {
        var result = await identity.ListMembersAsync(new BusinessId(businessId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpGet("api/businesses/{businessId:guid}")]
    public async Task<IActionResult> GetBusiness(Guid businessId, CancellationToken ct)
    {
        var result = await identity.GetBusinessAsync(new BusinessId(businessId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut("api/businesses/{businessId:guid}/invoice-template")]
    public async Task<IActionResult> SetInvoiceTemplate(Guid businessId, SetInvoiceTemplateRequest body, CancellationToken ct)
    {
        var result = await identity.SetInvoiceTemplateAsync(new BusinessId(businessId), body.LogoUrl,
            body.BusinessName, body.AccountNumber, body.BankName, body.Terms, ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpGet("api/businesses/{businessId:guid}/invoice-template")]
    public async Task<IActionResult> GetInvoiceTemplate(Guid businessId, CancellationToken ct)
    {
        var result = await identity.GetInvoiceTemplateAsync(new BusinessId(businessId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
