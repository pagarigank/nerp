// <copyright file="AccountController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/accounts")]
public class AccountController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public AccountController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAll([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var accounts = await _unitOfWork.Accounts.FindAsync(x => x.CompanyId == companyId, cancellationToken);
        return Ok(accounts.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, cancellationToken);
        if (account == null)
            return NotFound();

        return Ok(MapToDto(account));
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = new Account(
            request.CompanyId,
            request.AccountNumber,
            request.Description,
            request.AccountType,
            request.NormalBalance,
            request.IsActive);

        await _unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(Account),
            account.Id,
            "system",
            newValues: new { request.AccountNumber, request.Description },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, MapToDto(account));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Update(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, cancellationToken);
        if (account == null)
            return NotFound();

        account.Update(request.Description, request.AccountType, request.NormalBalance, request.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(account));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, cancellationToken);
        if (account == null)
            return NotFound();

        account.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static AccountDto MapToDto(Account account)
    {
        return new AccountDto(
            account.Id,
            account.CompanyId,
            account.AccountNumber,
            account.Description,
            account.AccountType,
            account.NormalBalance,
            account.IsActive,
            account.CreatedOn,
            account.ModifiedOn);
    }
}
