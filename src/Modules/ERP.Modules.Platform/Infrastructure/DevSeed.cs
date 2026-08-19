// <copyright file="DevSeed.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.Platform.Infrastructure;

/// <summary>
/// Idempotent local / development bootstrap. Creates an Admin role with a
/// wildcard permission set, a demo user (demo@erp.com / password123), and two
/// sample companies with an open fiscal period so the UI is usable without
/// external IdP provisioning. Every step checks for existing data first, so it
/// is safe to run on every startup and will not collide with data that was
/// already present (e.g. from a previous seed run). Disabled automatically
/// outside the Development environment.
/// </summary>
public static class DevSeed
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        // Ensure the schema exists (migrations are applied on startup in Dev).
        await db.Database.EnsureCreatedAsync(cancellationToken);

        // --- Permissions: common module/action permissions + wildcard ---
        var modules = new[] { "platform", "gl", "ap", "ar", "cash", "pur", "inv", "om" };
        var actions = new[] { "view", "create", "edit", "delete", "post", "approve" };

        var existingPermissions = await db.Permissions
            .Select(p => (p.Module + "." + p.Action).ToUpperInvariant())
            .ToListAsync(cancellationToken);

        var wantedPermissions = new List<(string Module, string Action, string Description)>();
        foreach (var m in modules)
        {
            foreach (var a in actions)
            {
                wantedPermissions.Add((m, a, $"{m} {a}"));
            }
        }

        wantedPermissions.Add(("*", "*", "Full access"));

        var missing = wantedPermissions
            .Where(w => !existingPermissions.Contains((w.Module + "." + w.Action).ToUpperInvariant()))
            .ToList();

        if (missing.Count > 0)
        {
            foreach (var w in missing)
            {
                db.Permissions.Add(new Permission(w.Module, w.Action, w.Description));
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        // --- Admin role (load-or-create) ---
        var adminRole = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

        if (adminRole is null)
        {
            adminRole = new Role("Admin", "Administrator with full access");
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);
        var adminPermissionIds = adminRole.Permissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var permissionId in allPermissionIds)
        {
            if (!adminPermissionIds.Contains(permissionId))
            {
                adminRole.AddPermission(permissionId);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // --- Demo user (load-or-create, ensure password is set) ---
        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == "demo@erp.com", cancellationToken);

        if (user is null)
        {
            user = new User("demo@erp.com", "demo@erp.com", "John Doe");
            db.Users.Add(user);
        }

        user.SetPassword(JwtTokenService.HashPassword("password123"));

        if (!user.Roles.Any(r => r.RoleId == adminRole.Id))
        {
            user.AddRole(adminRole.Id);
        }

        await db.SaveChangesAsync(cancellationToken);

        // --- Company-admin demo user (scoped to a single company) ---
        // "companyadmin@erp.com" / "password123" receives the Admin role but only
        // for the US Operations company, exercising the company-scoped path.
        var companyAdminUser = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == "companyadmin@erp.com", cancellationToken);

        if (companyAdminUser is null)
        {
            companyAdminUser = new User("companyadmin@erp.com", "companyadmin@erp.com", "Jane Smith");
            db.Users.Add(companyAdminUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        companyAdminUser.SetPassword(JwtTokenService.HashPassword("password123"));

        // The US company is created further down; attach the company-scoped role
        // once we know its id. Defer the assignment to after company creation.
        await db.SaveChangesAsync(cancellationToken);

        // --- Companies + fiscal periods (load-or-create) ---
        if (!await db.Companies.AnyAsync(c => c.Name == "US Operations", cancellationToken))
        {
            db.Companies.Add(new Company("US Operations", "ERP US Operations Inc.", "USD", "12-3456789", "123 Main St, New York, NY"));
        }

        if (!await db.Companies.AnyAsync(c => c.Name == "Canada Operations", cancellationToken))
        {
            db.Companies.Add(new Company("Canada Operations", "ERP Canada Operations Ltd.", "CAD", "98-7654321", "456 Queen St, Toronto, ON"));
        }

        await db.SaveChangesAsync(cancellationToken);

        var us = await db.Companies.FirstAsync(c => c.Name == "US Operations", cancellationToken);
        var ca = await db.Companies.FirstAsync(c => c.Name == "Canada Operations", cancellationToken);

        // Attach the company-scoped Admin role to the company-admin demo user
        // (only the US Operations company). This is idempotent.
        var companyAdmin = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == "companyadmin@erp.com", cancellationToken);
        if (companyAdmin is not null && !companyAdmin.Roles.Any(r => r.RoleId == adminRole.Id && r.CompanyId == us.Id))
        {
            companyAdmin.AddRole(adminRole.Id, us.Id);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.FiscalYears.AnyAsync(f => f.CompanyId == us.Id, cancellationToken))
        {
            db.FiscalYears.Add(new FiscalYear(
                us.Id,
                DateTimeOffset.UtcNow.Year,
                $"FY {DateTimeOffset.UtcNow.Year}",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero)));
        }

        if (!await db.FiscalYears.AnyAsync(f => f.CompanyId == ca.Id, cancellationToken))
        {
            db.FiscalYears.Add(new FiscalYear(
                ca.Id,
                DateTimeOffset.UtcNow.Year,
                $"FY {DateTimeOffset.UtcNow.Year}",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero)));
        }

        await db.SaveChangesAsync(cancellationToken);

        var usYear = await db.FiscalYears.FirstAsync(f => f.CompanyId == us.Id, cancellationToken);
        var caYear = await db.FiscalYears.FirstAsync(f => f.CompanyId == ca.Id, cancellationToken);

        if (!await db.FiscalPeriods.AnyAsync(p => p.CompanyId == us.Id && p.PeriodNumber == 7, cancellationToken))
        {
            db.FiscalPeriods.Add(new FiscalPeriod(
                usYear.Id,
                us.Id,
                7,
                "Jul 2026",
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 31, 0, 0, 0, TimeSpan.Zero)));
        }

        if (!await db.FiscalPeriods.AnyAsync(p => p.CompanyId == ca.Id && p.PeriodNumber == 7, cancellationToken))
        {
            db.FiscalPeriods.Add(new FiscalPeriod(
                caYear.Id,
                ca.Id,
                7,
                "Jul 2026",
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 31, 0, 0, 0, TimeSpan.Zero)));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
