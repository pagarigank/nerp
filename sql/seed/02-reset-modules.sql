-- =====================================================================
-- ERP Module Reset (hard delete of all module data)
-- Clears every row from the 14 business-module schemas plus the platform
-- business/reference tables, leaving ONLY the platform identity core
-- (Companies, Users, Roles, Permissions, UserRoles, RolePermissions)
-- so the demo login (demo@erp.com) and company scoping keep working.
--
-- Run:
--   sqlcmd -S localhost -U sa -P P@ssw0rd -C -d erp -b -i sql/seed/02-reset-modules.sql
--
-- NOTE: Hard delete. Not reversible. Re-run sql/seed/03-seed-masters.sql
-- afterwards to repopulate masters.
-- =====================================================================

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1) Disable all FK constraints so delete order does not matter.
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- 2) Delete every target table via a cursor (excludes the 6 identity tables).
DECLARE @sql NVARCHAR(MAX) = N'';
DECLARE @tbl NVARCHAR(300);

DECLARE cur CURSOR FOR
    SELECT '[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory'
      AND (
            TABLE_SCHEMA IN ('gl','ap','ar','cash','pur','inv','om','bom','proj','pay','fs')
         OR (TABLE_SCHEMA = 'platform'
             AND TABLE_NAME NOT IN ('Companies','Users','Roles','Permissions','UserRoles','RolePermissions'))
      )
    ORDER BY TABLE_SCHEMA, TABLE_NAME;

OPEN cur;
FETCH NEXT FROM cur INTO @tbl;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'SET QUOTED_IDENTIFIER ON; DELETE FROM ' + @tbl + N';';
    EXEC sp_executesql @sql;
    FETCH NEXT FROM cur INTO @tbl;
END
CLOSE cur;
DEALLOCATE cur;
GO

-- 3) Re-enable all FK constraints.
EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
GO

PRINT 'Module reset complete. Identity core (Companies/Users/Roles/Permissions) preserved.';
GO
