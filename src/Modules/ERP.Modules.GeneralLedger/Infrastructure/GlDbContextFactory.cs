// <copyright file="GlDbContextFactory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class GlDbContextFactory : IDesignTimeDbContextFactory<GlDbContext>
{
    public GlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GlDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=erp;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(GlDbContextFactory).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "gl");
            });

        return new GlDbContext(optionsBuilder.Options);
    }
}
