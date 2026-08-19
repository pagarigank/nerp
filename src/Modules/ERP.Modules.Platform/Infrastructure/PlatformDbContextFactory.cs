// <copyright file="PlatformDbContextFactory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP.Modules.Platform.Infrastructure;

public class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=erp;User Id=sa;Password=P@ssw0rd;TrustServerCertificate=True;MultipleActiveResultSets=True",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "platform");
            });

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
