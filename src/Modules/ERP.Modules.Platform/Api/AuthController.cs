// <copyright file="AuthController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1210

using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Api;

/// <summary>
/// Local username/password authentication. In production tokens are issued by
/// Azure AD (Entra ID) and validated by the JWT Bearer scheme; this controller
/// issues self-signed tokens via <see cref="JwtTokenService"/> for local / dev
/// use. The emitted token shape matches what the Entra scheme expects, so the
/// rest of the API's <c>[Authorize]</c> policies accept it unchanged.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly PlatformDbContext _db;
    private readonly JwtTokenService _tokenService;

    public AuthController(PlatformDbContext db, JwtTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<LoginResponse>.Failure(["Username and password are required."]));

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username, cancellationToken);

        if (user is null || !user.IsActive || !JwtTokenService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<LoginResponse>.Failure(["Invalid username or password."]));

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        var roles = await _db.Roles
            .Where(r => roleIds.Contains(r.Id) && r.IsActive)
            .Include(r => r.Permissions)
            .ToListAsync(cancellationToken);

        // Company scoping: a UserRole with CompanyId == null grants access to
        // every company (super admin). Otherwise the user is limited to the set
        // of companies referenced by their company-specific role assignments.
        var scopedCompanyIds = user.Roles
            .Where(ur => ur.CompanyId.HasValue)
            .Select(ur => ur.CompanyId!.Value)
            .Distinct()
            .ToList();
        var isSuperAdmin = user.Roles.Any(ur => !ur.CompanyId.HasValue);

        // A company administrator is a user who holds an admin-type role
        // (Admin / Administrator) that is scoped to a specific company. Such a
        // user may manage users/roles/settings for that company only.
        var adminRoleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "Administrator",
        };
        var isCompanyAdmin = user.Roles
            .Where(ur => ur.CompanyId.HasValue)
            .Any(ur => adminRoleNames.Contains(
                roles.FirstOrDefault(r => r.Id == ur.RoleId)?.Name ?? string.Empty));

        var permissionIds = roles.SelectMany(r => r.Permissions.Select(p => p.PermissionId)).Distinct().ToList();
        var permissionEntities = await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var roleDtos = roles.Select(r => new AuthRoleDto(
            r.Id.ToString(),
            r.Name,
            r.Description,
            r.Permissions.Select(rp => permissionEntities.FirstOrDefault(pe => pe.Id == rp.PermissionId))
                .Where(pe => pe is not null)
                .Select(pe => $"{pe!.Module}.{pe.Action}")
                .ToList())).ToList();

        var permissions = roleDtos.SelectMany(r => r.Permissions).Distinct().ToList();

        var token = _tokenService.GenerateToken(user.Id.ToString(), user.Username, user.DisplayName, roles.Select(r => r.Name).ToList(), permissions, isSuperAdmin, scopedCompanyIds, isCompanyAdmin);

        user.RecordLogin();
        await _db.SaveChangesAsync(cancellationToken);

        var today = DateTimeOffset.UtcNow;
        IQueryable<Company> companyQuery = _db.Companies.OrderBy(c => c.Name);
        if (!isSuperAdmin && scopedCompanyIds.Count > 0)
        {
            companyQuery = _db.Companies
                .Where(c => scopedCompanyIds.Contains(c.Id))
                .OrderBy(c => c.Name);
        }

        var companies = await companyQuery
            .Select(c => new AuthCompanyDto(
                c.Id.ToString(),
                c.Name,
                c.LegalName,
                c.TaxId ?? string.Empty,
                c.BaseCurrency,
                c.Address ?? string.Empty,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                null))
            .ToListAsync(cancellationToken);

        IQueryable<FiscalPeriod> periodsQuery = _db.FiscalPeriods.OrderBy(p => p.CompanyId).ThenBy(p => p.PeriodNumber);
        if (!isSuperAdmin && scopedCompanyIds.Count > 0)
        {
            periodsQuery = _db.FiscalPeriods
                .Where(p => scopedCompanyIds.Contains(p.CompanyId))
                .OrderBy(p => p.CompanyId).ThenBy(p => p.PeriodNumber);
        }

        var periods = await periodsQuery
            .Select(p => new AuthFiscalPeriodDto(
                p.Id.ToString(),
                p.CompanyId.ToString(),
                0,
                p.PeriodNumber,
                p.Description,
                p.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                p.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                today >= p.StartDate && today <= p.EndDate,
                p.Status == PeriodStatus.Closed))
            .ToListAsync(cancellationToken);

        var userDto = new AuthUserDto(
            user.Id.ToString(),
            user.Email,
            user.DisplayName,
            string.Empty,
            user.DisplayName,
            user.IsActive,
            roleDtos,
            permissions);

        var response = new LoginResponse(token, token, isSuperAdmin, userDto, companies, periods);
        var payload = ApiResponse<LoginResponse>.Success(response);
#pragma warning disable CA1869
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
#pragma warning restore CA1869
        Response.ContentType = "application/json";
        Response.ContentLength = Encoding.UTF8.GetByteCount(json);
        await Response.WriteAsync(json, cancellationToken);
        return new EmptyResult();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<AuthUserDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
        var name = User.FindFirstValue("name") ?? username;

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(User.FindAll("role").Select(c => c.Value))
            .Distinct().ToList();
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

        var userDto = new AuthUserDto(
            userId ?? "system",
            username ?? string.Empty,
            name ?? string.Empty,
            string.Empty,
            name ?? string.Empty,
            true,
            roles.Select(r => new AuthRoleDto(r, r, string.Empty, Array.Empty<string>())).ToList(),
            permissions);

        return Ok(ApiResponse<AuthUserDto>.Success(userDto));
    }
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    bool IsSuperAdmin,
    AuthUserDto User,
    IReadOnlyList<AuthCompanyDto> Companies,
    IReadOnlyList<AuthFiscalPeriodDto> FiscalPeriods);

public record AuthUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    IReadOnlyList<AuthRoleDto> Roles,
    IReadOnlyList<string> Permissions);

public record AuthRoleDto(string Id, string Name, string Description, IReadOnlyList<string> Permissions);

public record AuthCompanyDto(
    string Id, string Name, string LegalName, string TaxId,
    string BaseCurrency, string AddressLine1, string? AddressLine2, string City,
    string State, string PostalCode, string Country, string? Phone, string? Email);

public record AuthFiscalPeriodDto(
    string Id, string CompanyId, int FiscalYear, int PeriodNumber, string PeriodName,
    string StartDate, string EndDate, bool IsCurrent, bool IsClosed);
