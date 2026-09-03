// <copyright file="MalaysianTaxDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Database context for Malaysian Tax (SST) module.
/// Tables are in the 'tax' schema.
/// </summary>
public class MalaysianTaxDbContext : DbContext
{
    public MalaysianTaxDbContext(DbContextOptions<MalaysianTaxDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<CompanySstRegistration> CompanySstRegistrations => Set<CompanySstRegistration>();
    public DbSet<TaxDeterminationRule> TaxDeterminationRules => Set<TaxDeterminationRule>();
    public DbSet<SstReturn> SstReturns => Set<SstReturn>();
    public DbSet<ExemptSupplyCategory> ExemptSupplyCategories => Set<ExemptSupplyCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure schema is applied
        modelBuilder.HasDefaultSchema("tax");

        // TaxCode configuration
        modelBuilder.Entity<TaxCode>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.LhdntTaxType);
        });

        // TaxRate configuration
        modelBuilder.Entity<TaxRate>(entity =>
        {
            entity.HasIndex(e => e.EffectiveFrom);
            entity.HasIndex(e => e.TaxCodeId);
            entity.HasIndex(e => new { e.TaxCodeId, e.EffectiveFrom });
            
            // Ensure only one active rate per code at a time
            entity.HasQueryFilter(tr => tr.IsActive);
        });

        // CompanySstRegistration configuration
        modelBuilder.Entity<CompanySstRegistration>(entity =>
        {
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => new { e.CompanyId, e.IsActive });
        });

        // TaxDeterminationRule configuration
        modelBuilder.Entity<TaxDeterminationRule>(entity =>
        {
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.ItemCategory);
            entity.HasIndex(e => e.CustomerType);
            entity.HasIndex(e => e.PlaceOfSupply);
            entity.HasIndex(e => new { e.CompanyId, e.IsActive, e.Priority });
            entity.HasIndex(e => e.TaxCodeId);
        });

        // SstReturn configuration
        modelBuilder.Entity<SstReturn>(entity =>
        {
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => new { e.CompanyId, e.PeriodStart, e.ReturnType });
            entity.HasIndex(e => e.Status);
        });

        // ExemptSupplyCategory configuration
        modelBuilder.Entity<ExemptSupplyCategory>(entity =>
        {
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => new { e.CompanyId, e.CategoryCode }).IsUnique();
            entity.HasIndex(e => e.SupplyType);
            entity.HasIndex(e => e.TaxCodeId);
        });
    }
}