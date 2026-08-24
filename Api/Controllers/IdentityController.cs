using Bookkeeping.Application.Identity;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Bookkeeping.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookkeeping.Api.Controllers;

[ApiController]
public sealed class IdentityController(IIdentityService identity, IAuthService auth) : ControllerBase
{
    public sealed record RegisterUserRequest(string Email, string DisplayName, string Password);
    public sealed record RegisterBusinessWithOwnerRequest(
        string Email, string DisplayName, string Password, string BusinessName, BusinessSector Sector);
    public sealed record AddMemberRequest(string Email, string DisplayName, string Password, BusinessRole Role);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record LogoutRequest(string RefreshToken);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string NewPassword);
    public sealed record SendEmailConfirmationRequest(string Email);

    // Multipart form for the invoice template. Binding the whole form into one model
    // (rather than spreading [FromForm] across parameters) keeps Swagger able to
    // describe the file upload.
    public sealed class SetInvoiceTemplateForm
    {
        public IFormFile Logo { get; init; } = default!;
        public string BusinessName { get; init; } = string.Empty;
        public string AccountNumber { get; init; } = string.Empty;
        public string BankName { get; init; } = string.Empty;
        public string Terms { get; init; } = string.Empty;
    }

    // Logos are small; cap the upload so a large file can't be buffered into memory.
    private const long MaxLogoBytes = 2 * 1024 * 1024;

    [AllowAnonymous]
    [HttpPost("api/users")]
    public async Task<IActionResult> RegisterUser(RegisterUserRequest body, CancellationToken ct)
    {
        var result = await identity.RegisterUserAsync(body.Email, body.DisplayName, body.Password, ct);
        return result.IsSuccess
            ? Ok(new { userId = result.Value.Value })
            : BadRequest(new { error = result.Error });
    }

    [AllowAnonymous]
    [HttpPost("api/auth/login")]
    public async Task<IActionResult> Login(LoginRequest body, CancellationToken ct)
    {
        var result = await auth.SignInAsync(body.Email, body.Password, ct);
        return result.IsSuccess
            ? Ok(TokenResponse(result.Value))
            : BadRequest(new { error = result.Error });
    }

    // Anonymous: the caller's access token has usually expired by the time it refreshes,
    // so the refresh token in the body is the sole credential.
    [AllowAnonymous]
    [HttpPost("api/auth/refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest body, CancellationToken ct)
    {
        var result = await auth.RefreshAsync(body.RefreshToken, ct);
        return result.IsSuccess
            ? Ok(TokenResponse(result.Value))
            : BadRequest(new { error = result.Error });
    }

    // Anonymous and idempotent: revokes the session behind the given refresh token.
    // Doesn't depend on a live access token, so a client can log out even after its
    // access token has expired.
    [AllowAnonymous]
    [HttpPost("api/auth/logout")]
    public async Task<IActionResult> Logout(LogoutRequest body, CancellationToken ct)
    {
        await auth.LogoutAsync(body.RefreshToken, ct);
        return NoContent();
    }

    private static object TokenResponse(AuthTokens tokens) => new
    {
        accessToken = tokens.AccessToken,
        accessTokenExpiresAt = tokens.AccessTokenExpiresAt,
        refreshToken = tokens.RefreshToken,
        refreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
    };

    // Issues a password-reset token (as a callback URL). In production this URL is
    // emailed to the user rather than returned in the response; it is surfaced here
    // because no email sender is wired. Anonymous: a user who forgot their password
    // cannot be signed in.
    [AllowAnonymous]
    [HttpPost("api/auth/password-reset-token")]
    public async Task<IActionResult> RequestPasswordReset(ForgotPasswordRequest body, CancellationToken ct)
    {
        var result = await auth.GetPasswordResetTokenAsync(body.Email, ct);
        return result.IsSuccess
            ? Ok(new { resetUrl = result.Value })
            : BadRequest(new { error = result.Error });
    }

    // email and token ride in the query string (they come straight from the emailed
    // reset link); the new password stays in the body so it never lands in server
    // logs or browser history.
    [AllowAnonymous]
    [HttpPost("api/auth/reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromQuery] string email, [FromQuery] string token, ResetPasswordRequest body, CancellationToken ct)
    {
        var result = await auth.ResetPasswordAsync(email, token, body.NewPassword, ct);
        return result.IsSuccess
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    // Issues an email-confirmation token (as a callback URL); same emailing caveat as
    // the password-reset token above.
    [AllowAnonymous]
    [HttpPost("api/auth/email-confirmation-token")]
    public async Task<IActionResult> RequestEmailConfirmation(SendEmailConfirmationRequest body, CancellationToken ct)
    {
        var result = await auth.GetEmailConfirmationTokenAsync(body.Email, ct);
        return result.IsSuccess
            ? Ok(new { confirmationUrl = result.Value })
            : BadRequest(new { error = result.Error });
    }

    // Opened directly from the emailed confirmation link, so it's a GET and both
    // values arrive in the query string.
    [AllowAnonymous]
    [HttpGet("api/auth/confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token, CancellationToken ct)
    {
        var result = await auth.ConfirmEmailAsync(email, token, ct);
        return result.IsSuccess
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    // Combined signup: creates the owner and their business in one transaction.
    [AllowAnonymous]
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

    // The caller's own profile and memberships, keyed off the JWT subject. Any
    // authenticated user may read themselves, so no permission policy — just the
    // deny-by-default authentication baseline.
    [HttpGet("api/users/me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        // "sub" maps to NameIdentifier under the default inbound claim mapping; fall
        // back to the raw claim in case mapping is ever turned off.
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var result = await identity.GetCurrentUserAsync(new UserId(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.Users.Read)]
    [HttpGet("api/businesses/{businessId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid businessId, CancellationToken ct)
    {
        var result = await identity.ListMembersAsync(new BusinessId(businessId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.Business.Read)]
    [HttpGet("api/businesses/{businessId:guid}")]
    public async Task<IActionResult> GetBusiness(Guid businessId, CancellationToken ct)
    {
        var result = await identity.GetBusinessAsync(new BusinessId(businessId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    // Multipart: the logo is uploaded as a file and stored in object storage; the
    // other fields ride alongside as form values.
    [Authorize(Policy = Permissions.Business.Manage)]
    [HttpPut("api/businesses/{businessId:guid}/invoice-template")]
    [RequestSizeLimit(MaxLogoBytes)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SetInvoiceTemplate(
        Guid businessId, [FromForm] SetInvoiceTemplateForm form, CancellationToken ct)
    {
        var logo = form.Logo;
        if (logo is null || logo.Length == 0)
            return BadRequest(new { error = "A logo file is required." });
        if (logo.Length > MaxLogoBytes)
            return BadRequest(new { error = "Logo exceeds the 2 MB limit." });

        await using var stream = logo.OpenReadStream();
        var result = await identity.SetInvoiceTemplateAsync(new BusinessId(businessId),
            stream, logo.ContentType, form.BusinessName, form.AccountNumber, form.BankName, form.Terms, ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.Business.Read)]
    [HttpGet("api/businesses/{businessId:guid}/invoice-template")]
    public async Task<IActionResult> GetInvoiceTemplate(Guid businessId, CancellationToken ct)
    {
        var result = await identity.GetInvoiceTemplateAsync(new BusinessId(businessId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
