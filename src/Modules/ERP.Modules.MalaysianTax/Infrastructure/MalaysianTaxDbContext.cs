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
    }
}