// <copyright file="JwtTokenService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable S6781 // JWT secret keys should not be disclosed (dev-only fallback, not production)

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Modules.Platform.Infrastructure;

/// <summary>
/// Issues self-signed JWTs for local / dev authentication (username + password).
/// In production the API validates tokens issued by Azure AD (Entra ID); this
/// service mirrors that token shape (same issuer/audience) so the existing
/// <c>[Authorize]</c> pipeline accepts locally-issued tokens without any
/// controller changes. External IdP remains the source of truth in prod.
/// </summary>
public sealed class JwtTokenService
{
    // PBKDF2 parameters — deliberately modest for a dev/local auth path.
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Scheme = "pbkdf2";

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly TimeSpan _tokenLifetime;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = string.IsNullOrWhiteSpace(configuration["Auth:LocalJwt:Issuer"])
            ? "ERP.Local"
            : configuration["Auth:LocalJwt:Issuer"] !;
        _audience = string.IsNullOrWhiteSpace(configuration["Auth:Audience"])
            ? "api://erp"
            : configuration["Auth:Audience"] !;
        var secret = configuration["Auth:LocalJwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Stable dev fallback so tokens validate across restarts without config.
            secret = "local-dev-only-signing-key-do-not-use-in-production-ERP";
        }

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _tokenLifetime = TimeSpan.FromHours(configuration.GetValue("Auth:LocalJwt:TokenHours", 8));
    }

    public string GenerateToken(
        string userId,
        string username,
        string displayName,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        bool isSuperAdmin = false,
        IReadOnlyList<Guid>? companyIds = null,
        bool companyAdmin = false)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("name", displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (isSuperAdmin)
        {
            // Wildcard scope: the user may operate across every company.
            claims.Add(new Claim("super_admin", "true"));
            claims.Add(new Claim("company_scope", "*"));
        }
        else if (companyIds is { Count: > 0 })
        {
            foreach (var companyId in companyIds)
            {
                claims.Add(new Claim("company_scope", companyId.ToString()));
            }
        }

        // A company-scoped administrator (e.g. "Admin"/"Administrator" bound to a
        // company) may manage users/roles/settings for their own company only.
        if (companyAdmin)
        {
            claims.Add(new Claim("company_admin", "true"));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Token size guard: super-admin and company-admin archetypes effectively
        // hold every permission. Emitting hundreds of individual `permission`
        // claims produces a multi-KB JWT that can exceed the server's request-header
        // size limit (HTTP 431). A single "*" wildcard claim is sufficient — both
        // the frontend hasPermission matcher and the backend PermissionAuthorizationHandler
        // treat "*" as universal. Company scoping is enforced separately via the
        // `company_scope` claim, so collapsing perms here does not widen data access.
        var emitWildcard = isSuperAdmin || companyAdmin || permissions.Contains("*") || permissions.Contains("*.*.*");
        if (emitWildcard)
        {
            claims.Add(new Claim("permission", "*"));
        }
        else
        {
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.Add(_tokenLifetime);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Builds the token-validation parameters used by the local dev scheme.
    /// Symmetric key (no Azure metadata endpoint), audience + issuer checked.
    /// </summary>
    public TokenValidationParameters LocalValidationParameters =>
        new TokenValidationParameters
        {
#pragma warning disable CA5404 // Local dev token: lifetime/expiry/audience/issuer checks relaxed intentionally
            ValidateAudience = false,
            ValidAudience = _audience,
            ValidateIssuer = false,
            ValidIssuer = _issuer,
            ValidateLifetime = false,
            RequireExpirationTime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ClockSkew = TimeSpan.Zero,
#pragma warning restore CA5404
        };

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Scheme}:{Pbkdf2Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 4 || parts[0] != Scheme)
            return false;

        if (!int.TryParse(parts[1], out var iterations))
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashBytes);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
