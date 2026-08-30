// <copyright file="PermissionsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/permissions")]
[Authorize(Policy = "CompanyAdminOrSuper")]
public class PermissionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PermissionsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>All permissions (id + stable code) used to resolve the role editor selections.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var perms = await _unitOfWork.Permissions.GetAllAsync(cancellationToken);
        return Ok(perms.Select(p => new PermissionDto(p.Id, p.Module, p.Page, p.Action, p.Code, p.Description)).ToList());
    }
}
