using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain;
using Bookkeeping.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookkeeping.Api.Controllers;

// Connect and disconnect a business's bank accounts through the feed provider.
// Linking exchanges the widget's authorisation code for a durable account handle
// that the bank-imports endpoint pulls against.
[ApiController]
[Route("api/businesses/{businessId:guid}/bank-accounts")]
public sealed class BankAccountsController(IBankAccountService accounts) : ControllerBase
{
    public sealed record LinkRequest(string AuthorisationCode);

    [Authorize(Policy = Permissions.BankAccounts.Manage)]
    [HttpPost]
    public async Task<IActionResult> Link(Guid businessId, LinkRequest body, CancellationToken ct)
    {
        var result = await accounts.LinkAsync(new BusinessId(businessId), body.AuthorisationCode, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Policy = Permissions.BankAccounts.Read)]
    [HttpGet]
    public async Task<IActionResult> List(Guid businessId, CancellationToken ct)
        => Ok(await accounts.ListAsync(new BusinessId(businessId), ct));

    [Authorize(Policy = Permissions.BankAccounts.Manage)]
    [HttpDelete("{accountId:guid}")]
    public async Task<IActionResult> Unlink(Guid businessId, Guid accountId, CancellationToken ct)
    {
        var result = await accounts.UnlinkAsync(new BusinessId(businessId), new LinkedBankAccountId(accountId), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
